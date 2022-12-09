// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;

namespace Xrv.Core.Networking.Properties.KeyRequest.Messages
{
    internal class AssignedKeysMessage : RequestKeyProtocolMessage
    {
        public AssignedKeysMessage()
        {
            this.Type = RequestKeyMessageType.ServerAcceptKeysRequest;
        }

        public byte[] Keys { get; set; }

        public override void ReadFrom(IncomingMessage message)
        {
            base.ReadFrom(message);
            this.Keys = message.ReadBytes();
        }

        public override void WriteTo(OutgoingMessage message)
        {
            base.WriteTo(message);
            message.Write(this.Keys);
        }
    }
}
