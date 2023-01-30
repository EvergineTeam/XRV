// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    internal class StartProtocolRequestMessage : INetworkingMessageConverter
    {
        public string ProtocolName { get; set; }

        public void ReadFrom(IncomingMessage message)
        {
            this.ProtocolName = message.ReadString();
        }

        public void WriteTo(OutgoingMessage message)
        {
            message.Write(this.ProtocolName);
        }
    }
}
