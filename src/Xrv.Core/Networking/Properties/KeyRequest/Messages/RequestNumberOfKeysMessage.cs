// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Components;
using Evergine.Networking.Connection.Messages;

namespace Xrv.Core.Networking.Properties.KeyRequest.Messages
{
    internal class RequestNumberOfKeysMessage : RequestKeyProtocolMessage
    {
        public RequestNumberOfKeysMessage()
        {
            this.Type = RequestKeyMessageType.ClientRequestKeys;
        }

        public byte NumberOfKeys { get; set; }

        public NetworkPropertyProviderFilter ProviderType { get; set; }

        public override void ReadFrom(IncomingMessage message)
        {
            base.ReadFrom(message);
            this.NumberOfKeys = message.ReadByte();
            this.ProviderType = (NetworkPropertyProviderFilter)message.ReadByte();
        }

        public override void WriteTo(OutgoingMessage message)
        {
            base.WriteTo(message);
            message.Write(this.NumberOfKeys);
            message.Write((byte)this.ProviderType);
        }
    }
}
