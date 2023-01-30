// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using Evergine.Xrv.Core.Networking.Messaging;

namespace Evergine.Xrv.Core.Networking.Properties.Session
{
    internal class UpdateSessionDataRequestMessage : INetworkingMessageConverter
    {
        public UpdateSessionDataMessageType Type { get; set; }

        public virtual void ReadFrom(IncomingMessage message)
        {
        }

        public virtual void WriteTo(OutgoingMessage message)
        {
            message.Write((byte)this.Type);
        }
    }
}
