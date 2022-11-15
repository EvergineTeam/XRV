// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Networking.Client;
using Evergine.Networking.Server;
using Lidgren.Network;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Xrv.Core.UI.Tabs;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Network system.
    /// </summary>
    public class NetworkSystem
    {
        private readonly XrvService xrvService;
        private readonly AssetsService assetsService;
        private readonly SessionInfo session;
        private TabItem settingsItem;
        private bool networkingAvailable;
        private NetworkConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="assetsService">Assets service.</param>
        public NetworkSystem(XrvService xrvService, AssetsService assetsService)
        {
            this.xrvService = xrvService;
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
                    this.Client = new NetworkClient(this.Configuration);
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

        internal void RegisterServices()
        {
            if (!Application.Current.IsEditor)
            {
                var container = Application.Current.Container;
                container.RegisterInstance(new MatchmakingServerService());
                container.RegisterInstance(new MatchmakingClientService());
            }
        }

        internal void Load()
        {
            this.AddOrRemoveSettingItem();
            this.FixDefaultNetworkInterface();
        }

        internal async Task<bool> StartSessionAsync(string serverName)
        {
            this.Server = new NetworkServer(this.Configuration);
            await this.Server.StartAsync(serverName).ConfigureAwait(false);
            this.Session.CurrentUserIsHost = this.Server.IsStarted;
            if (this.Session.CurrentUserIsHost)
            {
                await this.ConnectToSessionAsync(this.Server.Host).ConfigureAwait(false);
            }

            return this.Server.IsStarted;
        }

        internal async Task<bool> ConnectToSessionAsync(SessionHostInfo host)
        {
            this.Session.Host = host;
            this.Session.Status = SessionStatus.Joining;

            // just to give enough time to show status changes to the user
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            bool succeeded = await this.Client.ConnectAsync(host).ConfigureAwait(false);
            if (!succeeded)
            {
                this.ClearSessionStatus();
                return false;
            }

            bool joined = await this.Client.JoinSessionAsync().ConfigureAwait(false);
            if (!joined)
            {
                this.ClearSessionStatus();
                return false;
            }

            this.Session.Status = SessionStatus.Joined;

            return true;
        }

        internal async Task LeaveSessionAsync()
        {
            this.Client.Disconnect();

            if (this.session.CurrentUserIsHost)
            {
                await this.Server.StopAsync().ConfigureAwait(false);
                this.Server = null;
            }

            this.ClearSessionStatus();
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

        private void ClearSessionStatus()
        {
            this.session.Host = null;
            this.session.Status = SessionStatus.Disconnected;
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

        private void InternalClient_ClientStateChanged(object sender, ClientStates state)
        {
            Debug.WriteLine($"Client state changed to {state}");

            if (state == ClientStates.Disconnected)
            {
                this.ClearSessionStatus();
            }
        }

        private void FixDefaultNetworkInterface()
        {
            // In previous development, we detected that our Lidgren fork
            // was not working in a deterministic way when calculating network interface
            // (internally used when creating server). We were struggling some days about what
            // was happening because HoloLens server was not reachable by other clients, and
            // we found this issue.
            bool isHoloLens = false;

#if UWP
            isHoloLens = Windows.ApplicationModel.Preview.Holographic.HolographicApplicationPreview.IsCurrentViewPresentedOnHolographicDisplay();
#endif
            if (!isHoloLens)
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
