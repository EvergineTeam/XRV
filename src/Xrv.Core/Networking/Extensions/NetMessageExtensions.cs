// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using System;

namespace Xrv.Core.Networking.Extensions
{
    internal static class NetMessageExtensions
    {
        public static void Write(this OutgoingMessage message, Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            message.Write(guidBytes);
        }

        public static Guid ReadGuid(this IncomingMessage message)
        {
            byte[] guidBytes = message.ReadBytes();
            return new Guid(guidBytes);
        }
    }
}
