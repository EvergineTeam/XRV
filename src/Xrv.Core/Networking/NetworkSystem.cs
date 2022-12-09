// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Networking.Client;
using Evergine.Networking.Components;
using Evergine.Networking.Server;
using Lidgren.Network;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Xrv.Core.Networking.Messaging;
using Xrv.Core.Networking.Properties.KeyRequest;
using Xrv.Core.Networking.Properties.Session;
using Xrv.Core.Services.QR;
using Xrv.Core.UI.Tabs;
using Xrv.Core.Utils;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Network system.
    /// </summary>
    public class NetworkSystem
    {
        private readonly XrvService xrvService;
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;
        private readonly SessionInfo session;
        private TabItem settingsItem;
        private bool networkingAvailable;
        private NetworkConfiguration configuration;
        private Entity worldCenterEntity;
        private SessionDataSynchronization sessionDataSync;
        private SessionDataUpdateManager sessionDataUpdater;

        private MatchmakingServerService server;
        private MatchmakingClientService client;
        private ProtocolOrchestatorService orchestator;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="entityManager">Entity manager.</param>
        /// <param name="assetsService">Assets service.</param>
        public NetworkSystem(
            XrvService xrvService,
            EntityManager entityManager,
            AssetsService assetsService)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
            this.assetsService = assetsService;
            this.session = new SessionInfo(xrvService.PubSub);
        }

        /// <summary>
        /// Gets or sets network configuration.
        /// </summary>
        public NetworkConfiguration Configuration
        {
            get => this.configuration;
            set
            {
                if (this.configuration != value)
                {
                    this.configuration = value;

                    this.UnsubscribeClientEvents();
                    this.Client = new NetworkClient(this.client, this.Configuration);
                    this.SubscribeClientEvents();
                }
            }
        }

        /// <summary>
        /// Gets network server, if present.
        /// </summary>
        public NetworkServer Server { get; private set; }

        /// <summary>
        /// Gets network client.
        /// </summary>
        public NetworkClient Client { get; private set; }

        /// <summary>
        /// Gets current session information.
        /// </summary>
        public SessionInfo Session { get => this.session; }

        /// <summary>
        /// Gets or sets port override value.
        /// If not null, it overrides server scanning port, that is obtained from
        /// <see cref="NetworkConfiguration.Port"/>. This is useful if you want to connect
        /// two applications in the same machine: OS will not let you to have to applications
        /// sharing the same port, so you have to set a different one for each one of the instances.
        /// </summary>
        public int? OverrideScanningPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if network sessions option is available.
        /// </summary>
        public bool NetworkingAvailable
        {
            get => this.networkingAvailable;
            set
            {
                if (this.networkingAvailable != value)
                {
                    this.networkingAvailable = value;
                    this.AddOrRemoveSettingItem();
                }
            }
        }

        internal IKeyStore KeyStore { get; private set; }

        internal SessionDataUpdateManager SessionDataUpdateManager { get => this.sessionDataUpdater; }

        internal IClientServerMessagingImpl ClientServerMessaging { get; set; }

        /// <summary>
        /// Adds an entity as child of world-center. To properly share
        /// entities in a networking session, they should be placed here.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void AddNetworkingEntity(Entity entity) =>
            this.worldCenterEntity.AddChild(entity);

        internal void RegisterServices()
        {
            if (!Application.Current.IsEditor)
            {
                var container = Application.Current.Container;
                this.server = new MatchmakingServerService();
                this.client = new MatchmakingClientService();
                container.RegisterInstance(this.server);
                container.RegisterInstance(this.client);
                this.InitializeKeyRequestProtocol(container);
                this.InitializeUpdateSessionDataProtocol();
            }
        }

        internal void Load()
        {
            this.AddOrRemoveSettingItem();
            this.FixDefaultNetworkInterface();

            this.sessionDataSync = new SessionDataSynchronization();
            this.sessionDataUpdater = new SessionDataUpdateManager();
            this.EnableSessionDataSync(false);
            this.session.SetData(this.sessionDataSync);

            this.worldCenterEntity = new Entity("Networking-WorldCenter")
                .AddComponent(new Transform3D())
                .AddComponent(new NetworkRoomProvider())
                .AddComponent(this.sessionDataSync)
                .AddComponent(this.sessionDataUpdater);
            this.entityManager.Add(this.worldCenterEntity);
        }

        internal async Task<bool> StartSessionAsync(string serverName)
        {
            this.Server = new NetworkServer(this.server, this.Configuration);
            await this.Server.StartAsync(serverName).ConfigureAwait(false);
            this.Session.CurrentUserIsHost = this.Server.IsStarted;
            if (this.Session.CurrentUserIsHost)
            {
                await this.ConnectToSessionAsync(this.Server.Host).ConfigureAwait(false);
            }

            return this.Server?.IsStarted ?? false;
        }

        internal async Task<bool> ConnectToSessionAsync(SessionHostInfo host)
        {
            this.Session.Host = host;
            this.Session.Status = SessionStatus.Joining;

            // just to give enough time to show status changes to the user
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            // close settings panel
            this.xrvService.Settings.Window.Close();

            // execute QR scanning flow to determine world center
            var scanningFlow = this.xrvService.Services.QrScanningFlow;
            QrScanningResult result = await scanningFlow.ExecuteFlowAsync().ConfigureAwait(false);
            if (result == null)
            {
                await this.ClearSessionStatusAsync().ConfigureAwait(false);
                return false;
            }

            bool succeeded = await this.Client.ConnectAsync(host).ConfigureAwait(false);
            if (!succeeded)
            {
                await this.ClearSessionStatusAsync().ConfigureAwait(false);
                return false;
            }

            bool joined = await this.Client.JoinSessionAsync().ConfigureAwait(false);
            if (!joined)
            {
                await this.ClearSessionStatusAsync().ConfigureAwait(false);
                return false;
            }

            this.Session.Status = SessionStatus.Joined;

            // Move world-center entity as child of QR scanning marker
            this.MoveWorldCenterEntity(scanningFlow, true);
            this.EnableSessionDataSync(true);

            return true;
        }

        internal Task LeaveSessionAsync()
        {
            this.Client.Disconnect();
            return Task.CompletedTask;
        }

        private void SubscribeClientEvents()
        {
            var internalClient = this.Client.InternalClient;
            internalClient.ClientStateChanged += this.InternalClient_ClientStateChanged;
        }

        private void UnsubscribeClientEvents()
        {
            var internalClient = this.Client?.InternalClient;
            if (internalClient == null)
            {
                return;
            }

            internalClient.ClientStateChanged -= this.InternalClient_ClientStateChanged;
        }

        private Entity GetSessionSettingsEntity()
        {
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.SessionSettings_weprefab);
            return rulerSettingPrefab.Instantiate();
        }

        private async Task ClearSessionStatusAsync()
        {
            this.session.Host = null;
            this.session.Status = SessionStatus.Disconnected;

            this.EnableSessionDataSync(false);

            var scanningFlow = this.xrvService.Services.QrScanningFlow;
            scanningFlow.Marker.IsEnabled = false;

            // Remove flow marker world anchor, if any
            var worldAnchor = scanningFlow.Marker.FindComponent<WorldAnchor>();
            worldAnchor.RemoveAnchor();

            if (this.Server?.IsStarted == true)
            {
                await this.Server.StopAsync().ConfigureAwait(false);
            }

            this.MoveWorldCenterEntity(scanningFlow, false);
            this.KeyStore.Clear();

            this.Server = null;
        }

        private void AddOrRemoveSettingItem()
        {
            var settings = this.xrvService.Settings;
            var general = settings.Window.Tabs.First(tab => tab.Name == "General");

            if (this.networkingAvailable)
            {
                this.settingsItem = new TabItem
                {
                    Order = general.Order + 1,
                    Name = "Sessions",
                    Contents = () => this.GetSessionSettingsEntity(),
                };
                settings.AddTabItem(this.settingsItem);
            }
            else
            {
                settings.RemoveTabItem(this.settingsItem);
                this.settingsItem = null;
            }
        }

        private async void InternalClient_ClientStateChanged(object sender, ClientStates state)
        {
            Debug.WriteLine($"Client state changed to {state}");

            if (state == ClientStates.Disconnected)
            {
                await this.ClearSessionStatusAsync();
            }
        }

        private void MoveWorldCenterEntity(QrScanningFlow scanningFlow, bool markerAsRoot)
        {
            var worldCenterTransform = this.worldCenterEntity.FindComponent<Transform3D>();
            worldCenterTransform.LocalTransform = Matrix4x4.CreateFromTRS(Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1));

            if (this.worldCenterEntity.Parent != null)
            {
                this.worldCenterEntity.Parent.DetachChild(this.worldCenterEntity);
            }
            else
            {
                this.entityManager.Detach(this.worldCenterEntity);
            }

            if (markerAsRoot)
            {
                scanningFlow.Marker.AddChild(this.worldCenterEntity);
            }
            else
            {
                this.entityManager.Add(this.worldCenterEntity);
            }
        }

        private void InitializeKeyRequestProtocol(Container container)
        {
            // TODO: refactoring everything related with keys and session data to a separated class
            var clientServerMessaging = new ClientServerMessagingImpl(this.server, this.client);
            this.orchestator = new ProtocolOrchestatorService(clientServerMessaging);
            this.orchestator.RegisterProtocolInstantiator(
                KeyRequestProtocol.ProtocolName,
                () => new KeyRequestProtocol(this));

            clientServerMessaging.IncomingMessageCallback = this.orchestator.HandleIncomingMessage; // TODO review this cycle reference :s
            clientServerMessaging.Orchestator = this.orchestator; // TODO review this cycle reference :s
            var keyStore = new KeyStore();
            keyStore.ReserveKeysForCore(new byte[] { default, SessionDataSynchronization.NetworkingKey });

            container.RegisterInstance(clientServerMessaging);
            container.RegisterInstance(this.orchestator);
            container.RegisterInstance(keyStore);

            this.KeyStore = keyStore;
            this.ClientServerMessaging = clientServerMessaging;
        }

        private void InitializeUpdateSessionDataProtocol()
        {
            this.orchestator.RegisterProtocolInstantiator(
                UpdateSessionDataProtocol.ProtocolName,
                () => new UpdateSessionDataProtocol(this, this.SessionDataUpdateManager));
        }

        private void EnableSessionDataSync(bool enabled)
        {
            this.sessionDataSync.IsEnabled = enabled;
            this.sessionDataUpdater.IsEnabled = enabled;
        }

        private void FixDefaultNetworkInterface()
        {
            // In previous development, we detected that our Lidgren fork
            // was not working in a deterministic way when calculating network interface
            // (internally used when creating server). We were struggling some days about what
            // was happening because HoloLens server was not reachable by other clients, and
            // we found this issue.
            if (!DeviceHelper.IsHoloLens())
            {
                NetworkInterface candidate = NetUtility
                    .GetNetworkInterfaces()
                    .FirstOrDefault(x => x?.GetIPProperties().GatewayAddresses.Count > 0);
                if (candidate != null)
                {
                    NetUtility.DefaultNetworkInterface = candidate;
                }
            }
        }
    }
}
