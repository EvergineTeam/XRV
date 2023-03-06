// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Linq;
#if !ANDROID
using System.Net.NetworkInformation;
#endif
using System.Threading.Tasks;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.Networking.Client;
using Evergine.Networking.Components;
using Evergine.Networking.Server;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Networking.ControlRequest;
using Evergine.Xrv.Core.Networking.Messaging;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Evergine.Xrv.Core.Networking.Properties.Session;
using Evergine.Xrv.Core.Services.QR;
using Evergine.Xrv.Core.Settings;
using Evergine.Xrv.Core.UI.Tabs;
#if !ANDROID
using Evergine.Xrv.Core.Utils;
using Lidgren.Network;
#endif
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Network system.
    /// </summary>
    public class NetworkSystem
    {
        private readonly XrvService xrvService;
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;
        private readonly LocalizationService localization;
        private readonly ILogger logger;

        private TabItem settingsItem;
        private bool networkingAvailable;
        private NetworkConfiguration configuration;
        private Entity worldCenterEntity;
        private SessionDataSynchronization sessionDataSync;
        private SessionDataUpdateManager sessionDataUpdater;
        private MatchmakingServerService server;
        private MatchmakingClientService client;
        private ProtocolOrchestatorService orchestator;
        private MenuButtonDescription controlRequestButtonDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="entityManager">Entity manager.</param>
        /// <param name="assetsService">Assets service.</param>
        /// <param name="logger">Logger.</param>
        public NetworkSystem(
            XrvService xrvService,
            EntityManager entityManager,
            AssetsService assetsService,
            ILogger logger)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
            this.assetsService = assetsService;
            this.localization = xrvService.Localization;
            this.logger = logger;

            this.controlRequestButtonDescription = new MenuButtonDescription
            {
                TextOn = () => this.localization.GetString(() => Resources.Strings.Networking_Menu_RequestControl),
                IconOn = CoreResourcesIDs.Materials.Icons.ControlRequest,
                Order = int.MaxValue - 2,
            };
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
                    this.Client = new NetworkClient(this.client, this.Configuration, this.logger);
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
        public SessionInfo Session { get; internal set; }

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

        /// <summary>
        /// Gets a value indicating whether TODO: Remove when networking is ready.
        /// </summary>
        internal static bool NetworkSystemEnabled { get; } = false;

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
                this.Session = new SessionInfo(this.client, this.xrvService.Services.Messaging);

                container.RegisterInstance(this.server);
                container.RegisterInstance(this.client);
                this.InitializeKeyRequestProtocol(container);
                this.InitializeUpdateSessionDataProtocol();
                this.InitializeControlRequestProtocol();
            }
        }

        internal void Load()
        {
            if (NetworkSystemEnabled)
            {
                this.AddOrRemoveSettingItem();
                this.FixDefaultNetworkInterface();
            }

            this.sessionDataSync = new SessionDataSynchronization();
            this.sessionDataUpdater = new SessionDataUpdateManager();
            this.EnableSessionDataSync(false);
            this.Session.SetData(this.sessionDataSync);

            this.worldCenterEntity = new Entity("Networking-WorldCenter")
                .AddComponent(new Transform3D())
                .AddComponent(new NetworkRoomProvider())
                .AddComponent(this.sessionDataSync)
                .AddComponent(this.sessionDataUpdater)
                .AddComponent(new SessionPresenterObserver());
            this.entityManager.Add(this.worldCenterEntity);
            this.xrvService.Services.Messaging.Subscribe<HandMenuActionMessage>(this.OnHandMenuButtonPressed);
        }

        internal async Task<bool> StartSessionAsync(string serverName)
        {
            this.Server = new NetworkServer(this.server, this.Configuration, this.logger);
            await this.Server.StartAsync(serverName).ConfigureAwait(false);
            if (this.Server.IsStarted)
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

            var handMenu = this.xrvService.HandMenu;
            handMenu.ButtonDescriptions.Add(this.controlRequestButtonDescription);
            var controlRequestButton = handMenu.GetButtonEntity(this.controlRequestButtonDescription);
            controlRequestButton.AddComponentIfNotExists(new HandMenuButtonStateUpdater());

            // Move world-center entity as child of QR scanning marker
            await this.MoveWorldCenterEntityAsync(scanningFlow, true).ConfigureAwait(false);
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
            this.Session.Host = null;
            this.Session.Status = SessionStatus.Disconnected;

            var handMenu = this.xrvService.HandMenu;
            handMenu.ButtonDescriptions.Remove(this.controlRequestButtonDescription);

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

            await this.MoveWorldCenterEntityAsync(scanningFlow, false).ConfigureAwait(false);
            this.KeyStore.Clear();

            this.Server = null;
        }

        private void AddOrRemoveSettingItem()
        {
            var settings = this.xrvService.Settings;
            var general = settings.Window.Tabs.First(tab => tab.Data as string == SettingsSystem.GeneralTabData);

            if (this.networkingAvailable)
            {
                this.settingsItem = new TabItem
                {
                    Order = general.Order + 1,
                    Name = () => this.localization.GetString(() => Resources.Strings.Settings_Tab_Sessions),
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
            using (this.logger?.BeginScope("Network client state change"))
            {
                this.logger?.LogDebug($"Client state changed to {state}");

                if (state == ClientStates.Disconnected)
                {
                    await this.ClearSessionStatusAsync()
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                this.logger?.LogError(t.Exception, "Error clearing session");
                            }
                        });
                }
            }
        }

        private async Task MoveWorldCenterEntityAsync(QrScanningFlow scanningFlow, bool markerAsRoot)
        {
            var worldCenterTransform = this.worldCenterEntity.FindComponent<Transform3D>();
            worldCenterTransform.LocalTransform = Matrix4x4.CreateFromTRS(Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1));

            await EvergineForegroundTask.Run(
                () =>
                {
                    if (this.worldCenterEntity.Parent != null)
                    {
                        this.worldCenterEntity.Parent.DetachChild(this.worldCenterEntity);
                    }
                    else
                    {
                        this.entityManager.Detach(this.worldCenterEntity);
                    }
                }).ConfigureEvergineAwait(EvergineTaskContinueOn.Background);

            await EvergineForegroundTask.Run(() =>
            {
                if (markerAsRoot)
                {
                    scanningFlow.Marker.AddChild(this.worldCenterEntity);
                }
                else
                {
                    this.entityManager.Add(this.worldCenterEntity);
                }
            });
        }

        private void InitializeKeyRequestProtocol(Container container)
        {
            // TODO: refactoring everything related with keys and session data to a separated class
            var clientServerMessaging = new ClientServerMessagingImpl(this.server, this.client);
            this.orchestator = new ProtocolOrchestatorService(clientServerMessaging);
            this.orchestator.RegisterProtocolInstantiator(
                KeyRequestProtocol.ProtocolName,
                () => new KeyRequestProtocol(this, this.logger));

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
                () => new UpdateSessionDataProtocol(this, this.SessionDataUpdateManager, this.logger));
        }

        private void InitializeControlRequestProtocol()
        {
            this.orchestator.RegisterProtocolInstantiator(
                ControlRequestProtocol.ProtocolName,
                () => new ControlRequestProtocol(this, this.xrvService.WindowsSystem, this.SessionDataUpdateManager, this.xrvService.Localization, this.logger));
        }

        private void EnableSessionDataSync(bool enabled)
        {
            this.sessionDataSync.IsEnabled = enabled;
            this.sessionDataUpdater.IsEnabled = enabled;
        }

        private async void OnHandMenuButtonPressed(HandMenuActionMessage message)
        {
            if (message.Description == this.controlRequestButtonDescription)
            {
                using (this.logger?.BeginScope("Session control request button press"))
                {
                    if (this.Session.CurrentUserIsHost)
                    {
                        await this.TakeControlAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await this.TryRequestControlAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task TakeControlAsync()
        {
            using (this.logger?.BeginScope("Taking session control"))
            {
                try
                {
                    var requestProtocol = new ControlRequestProtocol(
                        this,
                        this.xrvService.WindowsSystem,
                        this.sessionDataUpdater,
                        this.xrvService.Localization,
                        this.logger);
                    await requestProtocol.TakeControlAsync().ConfigureAwait(false);
                    this.logger?.LogDebug($"Control took");
                }
                catch (Exception ex)
                {
                    this.logger?.LogError(ex, "Take control error");
                }
            }
        }

        private async Task TryRequestControlAsync()
        {
            // avoid requesting control to yourself
            var sessionData = this.Session.Data;
            if (sessionData == null || sessionData.PresenterId == this.Client.ClientId)
            {
                return;
            }

            try
            {
                var requestProtocol = new ControlRequestProtocol(
                    this,
                    this.xrvService.WindowsSystem,
                    this.sessionDataUpdater,
                    this.xrvService.Localization,
                    this.logger);
                bool accepted = await requestProtocol.RequestControlAsync()
                    .ConfigureAwait(false);
                this.logger?.LogDebug($"Control request result: {accepted}");
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Control request error");
            }
        }

        private void FixDefaultNetworkInterface()
        {
#if !ANDROID
            // In previous development, we detected that our Lidgren fork
            // was not working in a deterministic way when calculating network interface
            // (internally used when creating server). We were struggling some days about what
            // was happening because HoloLens server was not reachable by other clients, and
            // we found this issue.
            bool overrideDefaultNetworkInterface = !DeviceHelper.IsHoloLens();

            if (overrideDefaultNetworkInterface)
            {
                NetworkInterface candidate = NetUtility
                    .GetNetworkInterfaces()
                    .FirstOrDefault(x => x?.GetIPProperties().GatewayAddresses.Count > 0);
                if (candidate != null)
                {
                    NetUtility.DefaultNetworkInterface = candidate;
                }
            }
#endif
        }
    }
}
