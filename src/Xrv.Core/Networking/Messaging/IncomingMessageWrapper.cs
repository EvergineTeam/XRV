// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using System;
using Xrv.Core.Networking.Extensions;

namespace Xrv.Core.Networking.Messaging
{
    internal class IncomingMessageWrapper : IIncomingMessage
    {
        private readonly IncomingMessage message;

        public IncomingMessageWrapper(IncomingMessage message)
        {
            this.message = message;
            this.IsProtocol = message.ReadBoolean();
            this.LifecycleType = (LifecycleMessageType)message.ReadByte();
            this.CorrelationId = message.ReadGuid();

            if (this.LifecycleType == LifecycleMessageType.Talking)
            {
                this.Type = message.ReadByte();
            }
        }

        public bool IsProtocol { get; private set; }

        public Guid CorrelationId { get; private set; }

        public LifecycleMessageType LifecycleType { get; private set; }

        public INetworkPeer Sender { get; set; }

        public byte Type { get; }

        public void To<TMessage>(TMessage target)
            where TMessage : INetworkingMessageConverter =>
            target.ReadFrom(this.message);
    }
}
