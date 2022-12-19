// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using Lidgren.Network;

namespace Xrv.Core.Networking.Properties.Session
{
    internal class UpdateSessionGroupDataRequestMessage : UpdateSessionDataRequestMessage
    {
        public UpdateSessionGroupDataRequestMessage()
        {
            this.Type = UpdateSessionDataMessageType.UpdateGroupData;
        }

        public SessionDataGroup Data { get; set; }

        public override void ReadFrom(IncomingMessage message)
        {
            base.ReadFrom(message);

            var data = new SessionDataGroup();
            data.Read((NetBuffer)message.InnerMessage);
            this.Data = data;
        }

        public override void WriteTo(OutgoingMessage message)
        {
            base.WriteTo(message);
            this.Data.Write((NetBuffer)message.InnerMessage);
        }
    }
}
