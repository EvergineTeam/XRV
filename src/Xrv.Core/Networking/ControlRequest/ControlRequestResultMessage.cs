// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;

namespace Xrv.Core.Networking.ControlRequest
{
    internal class ControlRequestResultMessage : ControlRequestMessage
    {
        public ControlRequestResultMessage()
        {
            this.Type = ControlRequestMessageType.ControlRequestResult;
        }

        public bool Accepted { get; set; }

        public override void ReadFrom(IncomingMessage message)
        {
            base.ReadFrom(message);
            this.Accepted = message.ReadBoolean();
        }

        public override void WriteTo(OutgoingMessage message)
        {
            base.WriteTo(message);
            message.Write(this.Accepted);
        }
    }
}
