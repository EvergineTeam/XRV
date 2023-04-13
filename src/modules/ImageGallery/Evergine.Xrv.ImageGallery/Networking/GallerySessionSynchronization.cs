// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Modules.Networking;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.ImageGallery.Networking
{
    internal class GallerySessionSynchronization : ModuleSessionSync<GallerySessionData>
    {
        [BindService]
        private XrvService xrvService = null;

        private Entity windowEntity;
        private ILogger logger;

        public GallerySessionSynchronization(Entity windowEntity)
        {
            this.windowEntity = windowEntity;
        }

        public override string GroupName => nameof(ImageGalleryModule);

        public override GallerySessionData CreateInitialInstance() => new GallerySessionData();

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        protected override void OnModuleDataSynchronized(GallerySessionData data)
        {
            this.logger?.LogDebug($"Window key: {data.WindowTransformPropertyKey}");
            this.logger?.LogDebug($"Current image key: {data.CurrentImageIndexPropertyKey}");

            var galleryKeys = this.GetKeysComponent();
            galleryKeys?.SetKeys(new byte[] { data.WindowTransformPropertyKey, data.CurrentImageIndexPropertyKey });
        }

        protected override void OnSessionDisconnected()
        {
            base.OnSessionDisconnected();

            var galleryKeys = this.GetKeysComponent();
            galleryKeys?.Reset();
        }

        private GalleryKeysAssignation GetKeysComponent() =>
            this.windowEntity.FindComponentInChildren<GalleryKeysAssignation>();
    }
}
