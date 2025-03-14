﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Networking;
using Evergine.Networking.Server;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Network server for shared sessions.
    /// </summary>
    public class NetworkServer
    {
        private readonly NetworkConfiguration configuration;
        private readonly MatchmakingServerService server;
        private readonly ILogger logger;
        private readonly SemaphoreSlim livingStatusSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class.
        /// </summary>
        /// <param name="server">Matchmaking server.</param>
        /// <param name="configuration">Network configuration.</param>
        /// <param name="logger">Logger.</param>
        public NetworkServer(
            MatchmakingServerService server,
            NetworkConfiguration configuration,
            ILogger logger)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
            this.server = server;
            this.logger = logger;

            this.server.ApplicationIdentifier = configuration.ApplicationIdentifier;
            this.server.ClientApplicationVersion = configuration.ClientApplicationVersion;
            this.server.PingInterval = configuration.PingInterval;
            this.server.ConnectionTimeout = configuration.ConnectionTimeout;

            this.livingStatusSemaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Gets a value indicating whether server is started.
        /// </summary>
        public bool IsStarted { get; private set; }

        internal SessionHostInfo Host { get; private set; }

        /// <summary>
        /// Starts a network server.
        /// </summary>
        /// <param name="serverName">Server name.</param>
        /// <returns>A task.</returns>
        public async Task StartAsync(string serverName)
        {
            await this.livingStatusSemaphore.WaitAsync().ConfigureAwait(false);

            if (this.IsStarted)
            {
                return;
            }

            using (this.logger?.BeginScope("Starting server"))
            {
                try
                {
                    this.logger?.LogInformation($"Initializing network server with name '{serverName}'");
                    this.server.ServerName = serverName;

                    await this.server.StartAsync(this.configuration.Port).ConfigureAwait(false);
                    this.logger?.LogInformation($"Started server at port {this.configuration.Port}");
                    this.IsStarted = true;
                    this.Host = new SessionHostInfo(
                        serverName,
                        new NetworkEndpoint("127.0.0.1", this.configuration.Port));
                }
                finally
                {
                    this.livingStatusSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Stops current server instance.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task StopAsync()
        {
            await this.livingStatusSemaphore.WaitAsync().ConfigureAwait(false);

            if (!this.IsStarted)
            {
                return;
            }

            using (this.logger?.BeginScope("Stopping server"))
            {
                try
                {
                    this.logger?.LogInformation("Shutting down network server");
                    await this.server.ShutdownAsync().ConfigureAwait(false);
                    this.IsStarted = false;
                }
                finally
                {
                    this.livingStatusSemaphore.Release();
                }
            }
        }
    }
}
