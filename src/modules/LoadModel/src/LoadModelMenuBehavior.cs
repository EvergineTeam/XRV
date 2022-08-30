// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Xrv.LoadModel
{
    /// <summary>
    /// Menu behavior.
    /// </summary>
    public class LoadModelMenuBehavior : Behavior
    {
        [BindComponent]
        private Transform3D transform = null;

        /// <summary>
        /// Gets or sets the transform target.
        /// </summary>
        public Transform3D TargetTransform { get; set; }

        /// <summary>
        /// Gets or sets the model target.
        /// </summary>
        public Entity Target { get; set; }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            var camera = this.Managers.RenderManager.ActiveCamera3D;

            if (camera != null)
            {
                ////var from = this.TargetTransform.Position;
                ////var cameraDir = camera.Position - from;
                ////this.transform.Position = this.TargetTransform.Position + (cameraDir * 0.5f);

                var cameraPosition = camera.Position;
                var up = Vector3.Up;
                Quaternion.CreateFromLookAt(ref cameraPosition, ref up, out var q);
                this.transform.Orientation = q;
            }
        }
    }
}
