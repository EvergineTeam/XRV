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
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Modules.Networking;
using Evergine.Xrv.Core.Networking.ControlRequest;
using Evergine.Xrv.Core.Networking.Messaging;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Evergine.Xrv.Core.Networking.Properties.Session;
using Evergine.Xrv.Core.Networking.SessionClosing;
using Evergine.Xrv.Core.Networking.WorldCenter;
using Evergine.Xrv.Core.Settings;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;
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
        private readonly WindowsSystem windowsSystem;
        private readonly ILogger logger;

        private TabItem settingsItem;
        private bool networkingAvailable;
        private bool debuggingEnabled = false;
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

            this.windowsSystem = xrvService.WindowsSystem;
            this.controlRequestButtonDescription = new MenuButtonDescription
            {
                TextOn = () => this.localization.GetString(() => Resources.Strings.Networking_Menu_RequestControl),
                IconOn = CoreResourcesIDs.Materials.Icons.ControlRequest,
                Order = int.MaxValue - 2,
            };

#if ANDROID
            this.WorldCenterProvider = new ManualWorldCenterProvider(
                this.assetsService,
                this.entityManager,
                this.windowsSystem,
                this.localization,
                this);
#else
            this.WorldCenterProvider = new QrWorldCenterProvider(this.xrvService.Services.QrScanningFlow, this.assetsService);
#endif
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
                    this.CreateNetworkClientInstance();
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

        /// <summary>
        /// Gets or sets a value indicating whether network debugging is enabled.
        /// Debugging consist on specified connection timeout and ping values
        /// to unlimited ones, to make it easier to debug applications while
        /// connected to a networking session.
        /// </summary>
        public bool DebuggingEnabled
        {
            get => this.debuggingEnabled;

            set
            {
                if (this.debuggingEnabled != value)
                {
                    this.debuggingEnabled = value;
                    this.CreateNetworkClientInstance();
                }
            }
        }

        /// <summary>
        /// Gets or sets world center provider to be used when connecting to sessions.
        /// </summary>
        public IWorldCenterProvider WorldCenterProvider { get; set; }

        internal IKeyStore KeyStore { get; private set; }

        /// <summary>
        /// Gets a value indicating whether TODO: Remove when networking is ready.
        /// </summary>
        internal static bool NetworkSystemEnabled { get; } = true;

        internal SessionDataUpdateManager SessionDataUpdateManager { get => this.sessionDataUpdater; }

        internal IClientServerMessagingImpl ClientServerMessaging { get; set; }

        /// <summary>
        /// Adds an entity as child of world-center. To properly share
        /// entities in a networking session, they should be placed here.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public void AddNetworkingEntity(Entity entity) =>
            this.worldCenterEntity.AddChild(entity);

        /// <summary>
        /// Indicates that a module is aware of networking session. It will add
        /// set of components required for module data synchronization. A module that
        /// is aware of networking session, will behave in a different way that outside of
        /// a session. For example, their associated hand menu button will be disabled
        /// unless user takes session control.
        /// </summary>
        /// <typeparam name="TSessionData">Type of module session data.</typeparam>
        /// <param name="module">Target module.</param>
        /// <param name="sessionDataSync">Session data synchornization component.</param>
        public void SetUpModuleSynchronization<TSessionData>(Module module, ModuleSessionSync<TSessionData> sessionDataSync)
            where TSessionData : ModuleSessionData
        {
            var moduleEntity = new Entity($"{module.Name}_Networking")
                .AddComponent(new ModuleActivationSync(module))
                .AddComponent(sessionDataSync)
                .AddComponent(new ModuleActivationNetworkKey<TSessionData>());
            this.worldCenterEntity.AddChild(moduleEntity);
        }

        /// <summary>
        /// Sets world center pose.
        /// </summary>
        /// <param name="pose">New pose.</param>
        public void SetWorldCenterPose(Matrix4x4 pose)
        {
            var transform3d = this.worldCenterEntity.FindComponent<Transform3D>();
            transform3d.WorldTransform = Matrix4x4.CreateFromTRS(pose.Translation, pose.Orientation, Vector3.One);
            this.WorldCenterProvider.OnWorldCenterPoseUpdate(this.worldCenterEntity);

            var worldAnchor = this.worldCenterEntity.FindComponent<WorldAnchor>();
            worldAnchor.RemoveAnchor();
            worldAnchor.SaveAnchor();
        }

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
                this.InitializeSessionClosingProtocol();
            }
        }

        internal void Load()
        {
            if (NetworkSystemEnabled)
            {
                this.AddOrRemoveSettingItem();
                this.FixDefaultNetworkInterface();
            }

            if (this.DebuggingEnabled)
            {
                this.configuration.ConnectionTimeout = TimeSpan.MaxValue;
                this.configuration.PingInterval = TimeSpan.MaxValue;
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
                .AddComponent(new SessionPresenterObserver())
                .AddComponent(new WorldAnchor());
            this.entityManager.Add(this.worldCenterEntity);
            this.xrvService.Services.Messaging.Subscribe<HandMenuActionMessage>(this.OnHandMenuButtonPressed);
            this.xrvService.ThemesSystem.ThemeUpdated += this.ThemesSystem_ThemeUpdated;
        }

        internal async Task<bool> StartSessionAsync(string serverName)
        {
            this.Session.ActivelyClosedByClient = false;
            this.Session.ActivelyClosedByHost = false;
            this.Server = new NetworkServer(this.server, this.Configuration, this.logger);
            await this.Server.StartAsync(serverName).ConfigureAwait(false);

            if (this.Server.IsStarted)
            {
                await this.ConnectToSessionAsync(this.Server.Host).ConfigureAwait(false);
            }

            this.worldCenterEntity.AddComponentIfNotExists(new SessionPresenterStillAlive());

            return this.Server?.IsStarted ?? false;
        }

        internal async Task<bool> ConnectToSessionAsync(SessionHostInfo host)
        {
            this.Session.Host = host;
            this.Session.ActivelyClosedByClient = false;
            this.Session.ActivelyClosedByHost = false;
            this.Session.Status = SessionStatus.Joining;

            // just to give enough time to show status changes to the user
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            // close settings panel
            this.xrvService.Settings.Window.Close();

            // execute routine to get world center
            Matrix4x4? worldCenterPose = await this.WorldCenterProvider.GetWorldCenterPoseAsync().ConfigureAwait(false);
            if (worldCenterPose == null)
            {
                await this.ClearSessionStatusAsync().ConfigureAwait(false);
                return false;
            }

            // Update world-center position and invoke provider update, that may show a visual indicator
            this.SetWorldCenterPose(worldCenterPose.Value);

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

            this.EnableSessionDataSync(true);

            return true;
        }

        internal async Task LeaveSessionAsync()
        {
            if (this.Session.CurrentUserIsHost)
            {
                var broadcaster = new ProtocolBroadcaster<SessionClosingProtocol>(this, this.logger);
                await broadcaster.BroadcastAsync(
                    () => new SessionClosingProtocol(this, this.logger),
                    protocol => protocol.NotifyClosingToClientAsync()).ConfigureAwait(false);
            }

            this.Session.ActivelyClosedByClient = true;
            this.Client.Disconnect();
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
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Networking.SessionSettings_weprefab);
            return rulerSettingPrefab.Instantiate();
        }

        private async Task ClearSessionStatusAsync()
        {
            this.Session.Host = null;
            this.Session.Status = SessionStatus.Disconnected;

            _ = EvergineForegroundTask.Run(() =>
            {
                var handMenu = this.xrvService.HandMenu;
                handMenu.ButtonDescriptions.Remove(this.controlRequestButtonDescription);
            });

            this.EnableSessionDataSync(false);

            if (this.Server?.IsStarted == true)
            {
                await this.Server.StopAsync().ConfigureAwait(false);
            }

            this.KeyStore.Clear();
            this.Server = null;

            // Clean world-center stuff
            this.worldCenterEntity.FindComponent<WorldAnchor>().RemoveAnchor();
            this.WorldCenterProvider.CleanWorldCenterEntity(this.worldCenterEntity);

            // Remove other session-related elements
            this.worldCenterEntity.RemoveComponent<SessionPresenterStillAlive>();

            _ = EvergineForegroundTask.Run(() =>
            {
                if (this.Session.ActivelyClosedByHost)
                {
                    this.windowsSystem.ShowAlertDialog(
                        this.localization.GetString(() => Resources.Strings.Sessions_HostFinishedSession_Title),
                        this.localization.GetString(() => Resources.Strings.Sessions_HostFinishedSession_Message),
                        this.localization.GetString(() => Resources.Strings.Global_Accept));
                }
                else if (!this.Session.ActivelyClosedByClient)
                {
                    this.windowsSystem.ShowAlertDialog(
                        this.localization.GetString(() => Resources.Strings.Sessions_ConnectionLost_Title),
                        this.localization.GetString(() => Resources.Strings.Sessions_ConnectionLost_Message),
                        this.localization.GetString(() => Resources.Strings.Global_Accept));
                }
            });
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

        private void InitializeSessionClosingProtocol()
        {
            this.orchestator.RegisterProtocolInstantiator(
                SessionClosingProtocol.ProtocolName,
                () => new SessionClosingProtocol(this, this.logger));
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

        private void CreateNetworkClientInstance()
        {
            if (this.DebuggingEnabled)
            {
                this.Configuration.ConnectionTimeout = TimeSpan.MaxValue;
                this.Configuration.PingInterval = TimeSpan.MaxValue;
            }

            this.UnsubscribeClientEvents();
            this.Client = new NetworkClient(this.client, this.Configuration, this.logger);
            this.SubscribeClientEvents();
        }

        private void ThemesSystem_ThemeUpdated(object sender, ThemeUpdatedEventArgs args)
        {
            if (args.IsNewThemeInstance || args.UpdatedColor == ThemeColor.PrimaryColor3)
            {
                this.assetsService.UpdateHoloGraphicAlbedo(
                    CoreResourcesIDs.Materials.Services.QR.ScannerBorder,
                    this.xrvService.ThemesSystem.CurrentTheme.GetColor(ThemeColor.PrimaryColor3));
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
