// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;

namespace Evergine.Xrv.Core.Networking.Properties.Session
{
    internal class UpdateGlobalSessionDataRequestMessage : UpdateSessionDataRequestMessage
    {
        public UpdateGlobalSessionDataRequestMessage()
        {
            this.Type = UpdateSessionDataMessageType.UpdateGlobalData;
        }

        public string PropertyName { get; set; }

        public object PropertyValue { get; set; }

        public override void ReadFrom(IncomingMessage message)
        {
            base.ReadFrom(message);

            this.PropertyName = message.ReadString();
            this.ReadPropertyValue(message);
        }

        public override void WriteTo(OutgoingMessage message)
        {
            base.WriteTo(message);

            message.Write(this.PropertyName);
            this.WritePropertyValue(message);
        }

        private void ReadPropertyValue(IncomingMessage message)
        {
            if (this.PropertyName == nameof(SessionData.PresenterId))
            {
                this.PropertyValue = message.ReadInt32();
            }
        }

        private void WritePropertyValue(OutgoingMessage message)
        {
            if (this.PropertyName == nameof(SessionData.PresenterId))
            {
                message.Write((int)this.PropertyValue);
            }
        }
    }
}
