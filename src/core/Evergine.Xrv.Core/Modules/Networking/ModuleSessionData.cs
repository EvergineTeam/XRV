// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Lidgren.Network;

namespace Evergine.Xrv.Core.Modules.Networking
{
    /// <summary>
    /// Session data associated to a module.
    /// </summary>
    public class ModuleSessionData : INetworkSerializable
    {
        /// <summary>
        /// Gets or sets property key assigned to module visibility.
        /// </summary>
        public byte VisibilityPropertyKey { get; set; }

        /// <inheritdoc/>
        public virtual void Read(NetBuffer buffer)
        {
            this.VisibilityPropertyKey = buffer.ReadByte();
        }

        /// <inheritdoc/>
        public virtual void Write(NetBuffer buffer)
        {
            buffer.Write(this.VisibilityPropertyKey);
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"- Visibility key: {this.VisibilityPropertyKey}";
    }
}
