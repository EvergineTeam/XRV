// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Sends protocol messages to all connected clients.
    /// </summary>
    /// <typeparam name="TProtocol">Protocol type.</typeparam>
    public class ProtocolBroadcaster<TProtocol>
        where TProtocol : NetworkingProtocol
    {
        private readonly NetworkSystem network;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolBroadcaster{TProtocol}"/> class.
        /// </summary>
        /// <param name="network">Network system.</param>
        /// <param name="logger">Logger.</param>
        public ProtocolBroadcaster(NetworkSystem network, ILogger logger)
        {
            this.network = network;
            this.logger = logger;
        }

        /// <summary>
        /// Broadcasts protocol operation for all connected clients.
        /// </summary>
        /// <param name="protocolFactory">Factory to create protocol instances. A new
        /// instances will be run for each one of the clients.</param>
        /// <param name="protocolOperation">Invokes protocol operation to be executed.</param>
        /// <returns>A task.</returns>
        public async Task BroadcastAsync(Func<TProtocol> protocolFactory, Func<TProtocol, Task> protocolOperation)
        {
            var networkClient = this.network.Client.InternalClient;
            var remoteClients = networkClient.CurrentRoom.RemotePlayers.ToList();
            var operations = new List<Task>();

            foreach (var remote in remoteClients)
            {
                var protocol = protocolFactory.Invoke();
                protocol.TargetClientId = remote.Id;
                operations.Add(this.CreateOperationTask(protocol, protocolOperation));
            }

            await Task.WhenAll(operations).ConfigureAwait(false);
        }

        private Task CreateOperationTask(TProtocol protocol, Func<TProtocol, Task> protocolOperation)
        {
            this.logger?.LogDebug($"Broadcasting {protocol.Name} protocol to client {protocol.TargetClientId.Value}");
            return protocolOperation.Invoke(protocol);
        }
    }
}
