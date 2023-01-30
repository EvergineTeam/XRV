// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Lidgren.Network;
using System;

namespace Evergine.Xrv.Core.Networking.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="NetBuffer"/>.
    /// </summary>
    public static class NetBufferExtensions
    {
        /// <summary>
        /// Writes a GUID to a buffer.
        /// </summary>
        /// <param name="buffer">Buffer instance.</param>
        /// <param name="guid">GUID value.</param>
        public static void Write(this NetBuffer buffer, Guid guid)
        {
            var guidBytes = guid.ToByteArray();
            buffer.Write(guidBytes.Length);
            buffer.Write(guidBytes);
        }

        /// <summary>
        /// Reads a GUID from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer instance.</param>
        /// <returns>GUID value.</returns>
        public static Guid ReadGuid(this NetBuffer buffer)
        {
            int numberOfBytes = buffer.ReadInt32();
            byte[] guidBytes = buffer.ReadBytes(numberOfBytes);
            return new Guid(guidBytes);
        }
    }
}
