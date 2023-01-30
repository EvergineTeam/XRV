// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Networking.Client;
using Evergine.Networking.Connection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Scans for available network servers.
    /// </summary>
    public class SessionScanner : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private ILogger logger = null;
        private CancellationTokenSource scanningCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionScanner"/> class.
        /// </summary>
        public SessionScanner()
        {
            this.AvailableSessions = Enumerable.Empty<SessionHostInfo>();
        }

        /// <summary>
        /// Raised when a scanning cycle has been completed.
        /// </summary>
        public event EventHandler ScanningResultsUpdated;

        /// <summary>
        /// Gets available session hosts.
        /// </summary>
        [IgnoreEvergine]
        public IEnumerable<SessionHostInfo> AvailableSessions { get; private set; }

        /// <summary>
        /// Starts server scanning in local network.
        /// </summary>
        public void StartScanning()
        {
            this.scanningCts = new CancellationTokenSource();
            _ = this.StartScanningLoopAsync(this.scanningCts.Token);
        }

        /// <summary>
        /// Stops server scanning in local network.
        /// </summary>
        public void StopScanning()
        {
            this.scanningCts?.Cancel();
            this.scanningCts = null;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool isAttached = base.OnAttached();
            if (isAttached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return isAttached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.StartScanning();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.StopScanning();
        }

        private async Task StartScanningLoopAsync(CancellationToken cancellationToken)
        {
            NetworkClient client = this.xrvService.Networking.Client;
            NetworkConfiguration configuration = client.Configuration;
            int scanningPort = this.xrvService.Networking.OverrideScanningPort ?? configuration.Port;
            MatchmakingClientService internalClient = client.InternalClient;
            if (internalClient.IsConnected)
            {
                return;
            }

            using (this.logger?.BeginScope("Session scanning"))
            {
                do
                {
                    this.logger?.LogDebug("Servers discovering attempt...");

                    var sessions = new HashSet<SessionHostInfo>();
                    var handler = new EventHandler<HostDiscoveredEventArgs>((sender, e) =>
                    sessions.Add(new SessionHostInfo(e.ServerName, e.Host)));
                    internalClient.ServerDiscovered += handler;
                    internalClient.DiscoverServers(scanningPort);

                    this.logger?.LogDebug($"Waiting discovering results for {configuration.ServerScanInterval.TotalSeconds} seconds");
                    await Task.Delay(configuration.ServerScanInterval);
                    internalClient.ServerDiscovered -= handler;

                    this.AvailableSessions = sessions;
                    this.ScanningResultsUpdated?.Invoke(this, EventArgs.Empty);
                }
                while (!cancellationToken.IsCancellationRequested && !internalClient.IsConnected);
            }
        }
    }
}
