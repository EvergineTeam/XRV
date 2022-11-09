// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Evergine.Networking;
using Evergine.Networking.Server;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Network server for shared sessions.
    /// </summary>
    public class NetworkServer
    {
        private readonly NetworkConfiguration configuration;
        private readonly MatchmakingServerService server;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class.
        /// </summary>
        /// <param name="configuration">Network configuration.</param>
        public NetworkServer(NetworkConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
            this.server = new MatchmakingServerService()
            {
                ApplicationIdentifier = configuration.ApplicationIdentifier,
                ClientApplicationVersion = configuration.ClientApplicationVersion,
                PingInterval = configuration.PingInterval,
                ConnectionTimeout = configuration.ConnectionTimeout,
            };
        }

        /// <summary>
        /// Gets a value indicating whether server is started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets network configuration.
        /// </summary>
        public NetworkConfiguration Configuration { get => this.configuration; }

        internal MatchmakingServerService Service { get => this.server; }

        internal SessionHostInfo Host { get; private set; }

        /// <summary>
        /// Starts a network server.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <returns>A task.</returns>
        public async Task StartAsync(string serverName)
        {
            if (this.IsStarted)
            {
                return;
            }

            Debug.WriteLine($"Initializing network server with name '{serverName}'");
            this.server.ServerName = serverName;

            await this.server.StartAsync(this.configuration.Port).ConfigureAwait(false);
            Debug.WriteLine($"Started server at port {this.configuration.Port}");
            this.IsStarted = true;
            this.Host = new SessionHostInfo(
                serverName,
                new NetworkEndpoint("127.0.0.1", this.configuration.Port));
        }

        /// <summary>
        /// Stops current server instance.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task StopAsync()
        {
            if (!this.IsStarted)
            {
                return;
            }

            Debug.WriteLine("Shutting down network server");
            await this.server.ShutdownAsync().ConfigureAwait(false);
            this.IsStarted = false;
        }
    }
}
