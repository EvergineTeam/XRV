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
        private GallerySessionData pendingSyncData;
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
                this.windowEntity.AttachableStateChanged += this.WindowEntity_AttachableStateChanged;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.windowEntity.AttachableStateChanged -= this.WindowEntity_AttachableStateChanged;
            this.pendingSyncData = null;
        }

        protected override void OnModuleDataSynchronized(GallerySessionData data)
        {
            this.logger?.LogDebug($"Window key: {data.WindowTransformPropertyKey}");
            this.logger?.LogDebug($"Current image key: {data.CurrentImageIndexPropertyKey}");

            // As GalleryKeysAssignation is part of the prefab, sometimes it will be already loaded
            // at this point, sometimes not. This is why we copy session data and force keys assignation
            // once window contents are already loaded.
            if (this.windowEntity.IsActivated)
            {
                this.FindKeysComponentAndAssign(data);
            }
            else
            {
                this.pendingSyncData = data;
            }
        }

        protected override void OnSessionDisconnected()
        {
            base.OnSessionDisconnected();

            this.pendingSyncData = null;

            var galleryKeys = this.GetKeysComponent();
            galleryKeys?.Reset();
        }

        private GalleryKeysAssignation GetKeysComponent() =>
            this.windowEntity.FindComponentInChildren<GalleryKeysAssignation>();

        private void FindKeysComponentAndAssign(GallerySessionData data)
        {
            var galleryKeys = this.GetKeysComponent();
            galleryKeys?.SetKeys(new byte[] { data.WindowTransformPropertyKey, data.CurrentImageIndexPropertyKey });
        }

        private void WindowEntity_AttachableStateChanged(object sender, AttachableObjectState state)
        {
            if (state == AttachableObjectState.Activated && this.pendingSyncData != null)
            {
                this.FindKeysComponentAndAssign(this.pendingSyncData);
            }

            this.pendingSyncData = null;
        }
    }
}
