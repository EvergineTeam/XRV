// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Client server messaging for protocols.
    /// </summary>
    public interface IClientServerMessaging
    {
        /// <summary>
        /// Registers a protocol as self-protocol: this is, a protocol that
        /// execution is started from current device.
        /// </summary>
        /// <param name="protocol">Protocol instance.</param>
        void RegisterSelfProtocol(NetworkingProtocol protocol);

        /// <summary>
        /// Unregisters a protocol as self-protocol.
        /// </summary>
        /// <param name="protocol">Protocol instance.</param>
        void UnregisterSelfProtocol(NetworkingProtocol protocol);

        /// <summary>
        /// Sends a protocol message to the server.
        /// </summary>
        /// <param name="protocol">Protocol instance.</param>
        /// <param name="message">Protocol message.</param>
        void SendProtocolMessageToServer(NetworkingProtocol protocol, INetworkingMessageConverter message);

        /// <summary>
        /// Sends a protocol message to a client.
        /// </summary>
        /// <param name="protocol">Protocol instance.</param>
        /// <param name="message">Protocol message.</param>
        /// <param name="clientId">Target client identifier.</param>
        void SendProtocolMessageToClient(NetworkingProtocol protocol, INetworkingMessageConverter message, int clientId);
    }
}
