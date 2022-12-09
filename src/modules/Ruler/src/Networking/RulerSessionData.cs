// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Lidgren.Network;

namespace Xrv.Ruler.Networking
{
    /// <summary>
    /// Networking session data relative to ruler module.
    /// </summary>
    public class RulerSessionData : INetworkSerializable
    {
        /// <summary>
        /// Gets property key assigned to ruler visibility.
        /// </summary>
        public byte VisibilityPropertyKey { get; internal set; }

        /// <summary>
        /// Gets property key assigned to first ruler handle.
        /// </summary>
        public byte Handle1PropertyKey { get; internal set; }

        /// <summary>
        /// Gets property key assigned to second ruler handle.
        /// </summary>
        public byte Handle2PropertyKey { get; internal set; }

        /// <inheritdoc/>
        public void Read(NetBuffer buffer)
        {
            this.VisibilityPropertyKey = buffer.ReadByte();
            this.Handle1PropertyKey = buffer.ReadByte();
            this.Handle2PropertyKey = buffer.ReadByte();
        }

        /// <inheritdoc/>
        public void Write(NetBuffer buffer)
        {
            buffer.Write(this.VisibilityPropertyKey);
            buffer.Write(this.Handle1PropertyKey);
            buffer.Write(this.Handle2PropertyKey);
        }
    }
}
