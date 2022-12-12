// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using System;

namespace Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Protocol lifecycle messaging.
    /// </summary>
    internal interface ILifecycleMessaging
    {
        /// <summary>
        /// Sends a lifecycle message to the server.
        /// </summary>
        /// <param name="correlationId">Protocol correlation identifier.</param>
        /// <param name="type">Message type.</param>
        /// <param name="beforeSending">Write to output buffer before sending.</param>
        void SendLifecycleMessageToServer(Guid correlationId, LifecycleMessageType type, Action<OutgoingMessage> beforeSending = null);

        /// <summary>
        /// Sends a lifecycle message to a client.
        /// </summary>
        /// <param name="correlationId">Protocol correlation identifier.</param>
        /// <param name="type">Message type.</param>
        /// <param name="useServerRole">Send message as server.</param>
        /// <param name="targetClientId">Destination client identifier.</param>
        /// <param name="beforeSending">Write to output buffer before sending.</param>
        void SendLifecycleMessageToClient(Guid correlationId, LifecycleMessageType type, bool useServerRole, int targetClientId, Action<OutgoingMessage> beforeSending = null);
    }
}
