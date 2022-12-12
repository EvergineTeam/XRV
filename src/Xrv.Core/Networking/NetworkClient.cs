// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Evergine.Networking.Client;
using Evergine.Networking.Messages;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Network client to connect to a network server for shared sessions.
    /// </summary>
    public class NetworkClient
    {
        private const string DefaultRoomName = "Room";

        private readonly NetworkConfiguration configuration;
        private readonly MatchmakingClientService client;
        private SessionHostInfo serverHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkClient"/> class.
        /// </summary>
        /// <param name="client">Matchmaking client.</param>
        /// <param name="configuration">Network configuration.</param>
        public NetworkClient(MatchmakingClientService client, NetworkConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.configuration = configuration;
            this.client = client;
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

        internal MatchmakingClientService InternalClient { get => this.client; }

        internal async Task<bool> ConnectAsync(SessionHostInfo host)
        {
            Debug.WriteLine($"Connecting to server at {host.Endpoint}");
            bool connected = await this.client.ConnectAsync(host.Endpoint).ConfigureAwait(false);
            Debug.WriteLine($"Client connection attempt: {(connected ? "suceeded" : "failed")}");
            this.serverHost = host;

            return connected;
        }

        internal async Task<bool> JoinSessionAsync()
        {
            if (this.serverHost == null)
            {
                return false;
            }

            Debug.WriteLine("Joining room");

            var options = new RoomOptions
            {
                RoomName = DefaultRoomName,
            };
            EnterRoomResultCodes joinResult = await this.client.JoinOrCreateRoomAsync(options).ConfigureAwait(false);
            var joined = joinResult == EnterRoomResultCodes.Succeed;
            Debug.WriteLine($"Join result: {joined}");

            if (joined)
            {
                Debug.WriteLine($"Joining room '{options.RoomName}' succeeded");
            }
            else
            {
                Debug.WriteLine($"Joining room '{options.RoomName}' failed");
            }

            return joined;
        }

        internal void Disconnect()
        {
            if (!this.IsConnected)
            {
                return;
            }

            Debug.WriteLine($"Disconnecting from host {this.serverHost}");
            this.client.Disconnect();
            this.serverHost = null;
        }
    }
}
