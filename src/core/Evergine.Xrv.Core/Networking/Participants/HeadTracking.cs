// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class HeadTracking : Behavior
    {
        [BindComponent(source: BindComponentSource.ParentsSkipOwner)]
        private Transform3D parentTransform = null;

        [BindComponent]
        private Transform3D transform = null;

        private Camera3D targetCamera;
        private Transform3D cameraTransform;

        protected override void Update(TimeSpan gameTime)
        {
            var currentCamera = this.Managers.RenderManager?.ActiveCamera3D;
            if (this.targetCamera != currentCamera)
            {
                this.targetCamera = currentCamera;
                this.cameraTransform = currentCamera?.Transform;
            }

            if (this.targetCamera == null || this.cameraTransform == null)
            {
                return;
            }

            var cameraWorldMatrix = Matrix4x4.CreateFromTRS(
                this.cameraTransform.Position,
                this.cameraTransform.Orientation,
                Vector3.One);

            this.transform.WorldTransform = cameraWorldMatrix * this.parentTransform.WorldInverseTransform;
        }
    }
}
