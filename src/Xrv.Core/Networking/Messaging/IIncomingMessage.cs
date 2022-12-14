// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Protocol incoming message.
    /// </summary>
    public interface IIncomingMessage
    {
        /// <summary>
        /// Gets a value indicating whether message if a protocol message.
        /// </summary>
        bool IsProtocol { get; }

        /// <summary>
        /// Gets protocol correlation identifier.
        /// </summary>
        Guid CorrelationId { get; }

        /// <summary>
        /// Gets lifecycle message type.
        /// </summary>
        internal LifecycleMessageType LifecycleType { get; }

        /// <summary>
        /// Gets protocol message type. This value is meaningful
        /// when <see cref="LifecycleType"/> value is <see cref="LifecycleMessageType.Talking"/>.
        /// </summary>
        byte Type { get; }

        /// <summary>
        /// Gets or sets message sender.
        /// </summary>
        INetworkPeer Sender { get; set; }

        /// <summary>
        /// Reads message buffer to fill protocol-specific message values.
        /// </summary>
        /// <typeparam name="TMessage">Protocol message type.</typeparam>
        /// <param name="target">Protocol message.</param>
        void To<TMessage>(TMessage target)
            where TMessage : INetworkingMessageConverter;
    }
}
