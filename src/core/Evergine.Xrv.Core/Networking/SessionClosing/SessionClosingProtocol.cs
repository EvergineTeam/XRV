// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.Networking.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.SessionClosing
{
    internal class SessionClosingProtocol : NetworkingProtocol
    {
        internal const string ProtocolName = "SessionClosing";

        private readonly NetworkSystem network;
        private readonly ILogger logger;

        public SessionClosingProtocol(NetworkSystem network, ILogger logger)
            : base(network, logger)
        {
            this.network = network;
            this.logger = logger;
        }

        public override string Name => ProtocolName;

        public async Task NotifyClosingToClientAsync()
        {
            if (!this.TargetClientId.HasValue)
            {
                throw new InvalidOperationException($"{nameof(this.TargetClientId)} requires a value");
            }

            using (this.logger?.BeginScope("Sending session closing message"))
            {
                await this.ExecuteAsync(() =>
                {
                    this.ClientServer.SendProtocolMessageToClient(this, new SessionClosingMessage(), this.TargetClientId.Value);
                    return Task.CompletedTask;
                });
            }
        }

        protected override INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message) =>
            new SessionClosingMessage();

        protected override void OnMessageReceived(INetworkingMessageConverter message, int senderId)
        {
            if (message is SessionClosingMessage)
            {
                this.logger?.LogInformation("Session terminated: host actively closed the session");
                this.network.Session.ActivelyClosedByHost = true;
            }
        }
    }
}
