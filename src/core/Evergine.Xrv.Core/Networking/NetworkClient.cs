// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Evergine.Networking.Client;
using Evergine.Networking.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Network client to connect to a network server for shared sessions.
    /// </summary>
    public class NetworkClient
    {
        private const string DefaultRoomName = "Room";

        private readonly NetworkConfiguration configuration;
        private readonly MatchmakingClientService client;
        private readonly ILogger logger;
        private SessionHostInfo serverHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class.
        /// </summary>
        /// <param name="client">Matchmaking client.</param>
        /// <param name="configuration">Network configuration.</param>
        /// <param name="logger">Logger.</param>
        public NetworkClient(
            MatchmakingClientService client,
            NetworkConfiguration configuration,
            ILogger logger)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
            this.client = client;
            this.logger = logger;

            this.client.ApplicationIdentifier = configuration.ApplicationIdentifier;
            this.client.ClientApplicationVersion = configuration.ClientApplicationVersion;
            this.client.PingInterval = configuration.PingInterval;
            this.client.ConnectionTimeout = configuration.ConnectionTimeout;
        }

        /// <summary>
        /// Gets network configuration.
        /// </summary>
        public NetworkConfiguration Configuration { get => this.configuration; }

        /// <summary>
        /// Gets a value indicating whether client is connected to a network server.
        /// </summary>
        public bool IsConnected { get => this.client?.IsConnected ?? false; }

        /// <summary>
        /// Gets current client identifier.
        /// </summary>
        public int ClientId { get => this.client?.LocalPlayer?.Id ?? -1; }

        internal MatchmakingClientService InternalClient { get => this.client; }

        internal async Task<bool> ConnectAsync(SessionHostInfo host)
        {
            bool connected = false;

            using (this.logger?.BeginScope("Client connection"))
            {
                this.logger?.LogInformation($"Connecting to server at {host.Endpoint}");
                connected = await this.client.ConnectAsync(host.Endpoint).ConfigureAwait(false);
                this.logger?.LogInformation($"Client connection attempt: {(connected ? "suceeded" : "failed")}");
                this.serverHost = host;
            }

            return connected;
        }

        internal async Task<bool> JoinSessionAsync()
        {
            if (this.serverHost == null)
            {
                return false;
            }

            bool joined = false;

            using (this.logger?.BeginScope("Client joining session"))
            {
                this.logger?.LogDebug("Joining room");

                var options = new RoomOptions
                {
                    RoomName = DefaultRoomName,
                };
                EnterRoomResultCodes joinResult = await this.client.JoinOrCreateRoomAsync(options).ConfigureAwait(false);
                joined = joinResult == EnterRoomResultCodes.Succeed;
                this.logger?.LogDebug($"Join result: {joined}");

                if (joined)
                {
                    this.logger?.LogDebug($"Joining room '{options.RoomName}' succeeded");
                }
                else
                {
                    this.logger?.LogDebug($"Joining room '{options.RoomName}' failed");
                }
            }

            return joined;
        }

        internal void Disconnect()
        {
            if (!this.IsConnected)
            {
                return;
            }

            using (this.logger?.BeginScope("Client disconnecting from server"))
            {
                this.logger?.LogInformation($"Disconnecting from host {this.serverHost}");
                this.client.Disconnect();
                this.serverHost = null;
            }
        }
    }
}
