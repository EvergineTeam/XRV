// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Reads or writes data using internal messaging buffers.
    /// </summary>
    public interface INetworkingMessageConverter
    {
        /// <summary>
        /// Writes a message contents to an outgoing message.
        /// </summary>
        /// <param name="message">Outgoing message.</param>
        void WriteTo(OutgoingMessage message);

        /// <summary>
        /// Reads a message contents from an incoming message.
        /// </summary>
        /// <param name="message">Incoming message.</param>
        void ReadFrom(IncomingMessage message);
    }
}
