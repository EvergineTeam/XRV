// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.Modules.Networking;
using Lidgren.Network;
using System.Text;

namespace Evergine.Xrv.Ruler.Networking
{
    /// <summary>
    /// Networking session data relative to ruler module.
    /// </summary>
    public class RulerSessionData : ModuleSessionData
    {
        /// <summary>
        /// Gets property key assigned to first ruler handle.
        /// </summary>
        public byte Handle1PropertyKey { get; internal set; }

        /// <summary>
        /// Gets property key assigned to second ruler handle.
        /// </summary>
        public byte Handle2PropertyKey { get; internal set; }

        /// <inheritdoc/>
        public override void Read(NetBuffer buffer)
        {
            base.Read(buffer);
            this.Handle1PropertyKey = buffer.ReadByte();
            this.Handle2PropertyKey = buffer.ReadByte();
        }

        /// <inheritdoc/>
        public override void Write(NetBuffer buffer)
        {
            base.Write(buffer);
            buffer.Write(this.Handle1PropertyKey);
            buffer.Write(this.Handle2PropertyKey);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder()
                .AppendLine(base.ToString())
                .AppendLine($"- Handle1 key: {this.Handle1PropertyKey}")
                .AppendLine($"- Handle2 key: {this.Handle2PropertyKey}");

            return builder.ToString();
        }
    }
}
