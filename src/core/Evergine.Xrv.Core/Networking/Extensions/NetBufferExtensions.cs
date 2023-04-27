// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;
using Evergine.Networking.Connection.Messages;
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

        /// <summary>
        /// Writes a nullable <see cref="Matrix4x4"/> to a buffer.
        /// </summary>
        /// <param name="buffer">Buffer instance.</param>
        /// <param name="matrix">Matrix value.</param>
        public static void WriteNullableMatrix4x4(this NetBuffer buffer, Matrix4x4? matrix)
        {
            buffer.Write(matrix.HasValue);
            if (matrix.HasValue)
            {
                buffer.Write(matrix.Value);
            }
        }

        /// <summary>
        /// Reads a nullable <see cref="Matrix4x4"/> from a buffer.
        /// </summary>
        /// <param name="buffer">Buffer instance.</param>
        /// <returns>Matrix value.</returns>
        public static Matrix4x4? ReadNullableMatrix4x4(this NetBuffer buffer)
        {
            bool hasValue = buffer.ReadBoolean();
            Matrix4x4? matrix = default;
            if (hasValue)
            {
                matrix = buffer.ReadMatrix4x4();
            }

            return matrix;
        }
    }
}
