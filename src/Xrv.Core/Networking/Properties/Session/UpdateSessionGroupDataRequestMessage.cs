// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using Lidgren.Network;
using Xrv.Core.Networking.Messaging;

namespace Xrv.Core.Networking.Properties.Session
{
    internal class UpdateSessionGroupDataRequestMessage : INetworkingMessageConverter
    {
        public UpdateSessionDataMessageType Type { get; set; }

        public SessionDataGroup Data { get; set; }

        public void ReadFrom(IncomingMessage message)
        {
            var data = new SessionDataGroup();
            data.Read((NetBuffer)message.InnerMessage);
            this.Data = data;
        }

        public void WriteTo(OutgoingMessage message)
        {
            message.Write((byte)this.Type);
            this.Data.Write((NetBuffer)message.InnerMessage);
        }
    }
}
