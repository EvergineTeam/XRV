// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Networking.Extensions;
using Evergine.Xrv.Core.Networking.Properties;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.ImageGallery.Networking
{
    /// <summary>
    /// It handles keys assignation for gallery module: module activation,
    /// gallery window transform and gallery current image.
    /// </summary>
    public class GalleryKeysAssignation : KeysAssignation
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Parents, isRequired: false)]
        private TransformSynchronization transformSync = null;

        [BindComponent(source: BindComponentSource.Scene, isRequired: false)]
        private GallerySessionSynchronization session = null;

        [BindComponent]
        private CurrentImageSynchronization currentImageSync = null;

        private ILogger logger;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnKeysAssigned(byte[] keys)
        {
            if (keys == null)
            {
                this.transformSync?.ResetKey();
                this.currentImageSync?.ResetKey();
                return;
            }

            this.logger?.LogDebug($"Gallery keys assigned. Result of {keys.Length} keys: {keys[0]}, {keys[1]}");
            this.session?.UpdateData(data =>
            {
                this.logger?.LogDebug($"Updating session data for image gallery keys: {keys[0]}, {keys[1]}");
                data.WindowTransformPropertyKey = keys[0];
                data.CurrentImageIndexPropertyKey = keys[1];
            });
        }

        /// <inheritdoc/>
        protected override void AssignKeysToProperties()
        {
            var keys = this.AssignedKeys;
            this.logger?.LogDebug($"Gallery keys established: {keys[0]}, {keys[1]}");
            this.transformSync.PropertyKeyByte = keys[0];
            this.currentImageSync.PropertyKeyByte = keys[1];
        }
    }
}
