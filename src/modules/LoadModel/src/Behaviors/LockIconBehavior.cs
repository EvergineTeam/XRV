// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Xrv.LoadModel
{
    /// <summary>
    /// Locked icon behavior is a simple behavior to follow the model.
    /// </summary>
    public class LockIconBehavior : Behavior
    {
        [BindComponent]
        private Transform3D transform = null;

        /// <summary>
        /// Gets or sets the transform target.
        /// </summary>
        public Transform3D ModelTransform { get; set; }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.ModelTransform == null)
            {
                return;
            }

            var camera = this.Managers.RenderManager.ActiveCamera3D;

            this.transform.Position = this.ModelTransform.Position;

            var cameraPosition = camera.Position - this.transform.Position;
            var up = Vector3.Up;
            Quaternion.CreateFromLookAt(ref cameraPosition, ref up, out Quaternion orientation);
            this.transform.Orientation = orientation;
        }
    }
}
