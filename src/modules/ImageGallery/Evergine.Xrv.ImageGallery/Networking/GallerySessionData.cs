// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.Modules.Networking;
using Lidgren.Network;
using System.Text;

namespace Evergine.Xrv.ImageGallery.Networking
{
    /// <summary>
    /// Networking session data for gallery module. It holds
    /// networking key values.
    /// </summary>
    public class GallerySessionData : ModuleSessionData
    {
        /// <summary>
        /// Gets or sets key used to synchronize window transform.
        /// </summary>
        public byte WindowTransformPropertyKey { get; set; }

        /// <summary>
        /// Gets or sets key used to synchronize current gallery image.
        /// </summary>
        public byte CurrentImageIndexPropertyKey { get; set; }

        /// <inheritdoc/>
        public override void Read(NetBuffer buffer)
        {
            base.Read(buffer);
            this.WindowTransformPropertyKey = buffer.ReadByte();
            this.CurrentImageIndexPropertyKey = buffer.ReadByte();
        }

        /// <inheritdoc/>
        public override void Write(NetBuffer buffer)
        {
            base.Write(buffer);
            buffer.Write(this.WindowTransformPropertyKey);
            buffer.Write(this.CurrentImageIndexPropertyKey);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder()
                .AppendLine(base.ToString())
                .AppendLine($"- Window transform key: {this.WindowTransformPropertyKey}")
                .AppendLine($"- Image index key: {this.CurrentImageIndexPropertyKey}");

            return builder.ToString();
        }
    }
}
