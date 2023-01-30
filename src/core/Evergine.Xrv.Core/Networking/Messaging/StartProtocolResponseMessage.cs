// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using System;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    internal class StartProtocolResponseMessage : INetworkingMessageConverter
    {
        public Guid CorrelationId { get; set; }

        public ProtocolError? ErrorCode { get; set; }

        public bool Succeeded { get => !this.ErrorCode.HasValue; }

        public void ReadFrom(IncomingMessage message)
        {
            var errorByte = message.ReadByte();
            this.ErrorCode = errorByte != 0 ? (ProtocolError)errorByte : null;
        }

        public void WriteTo(OutgoingMessage message)
        {
            message.Write((byte)(this.ErrorCode.HasValue ? this.ErrorCode : 0));
        }
    }
}
