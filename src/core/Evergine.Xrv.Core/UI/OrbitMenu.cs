// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.UI
{
    /// <summary>
    /// Moves menu around other entity (by its transform).
    /// </summary>
    public class OrbitMenu : Behavior
    {
        [BindComponent]
        private Transform3D transform = null;

        private float dampVelocitySeconds = 0.15f;
        private Vector3 velocity;
        private float dampVelocityOrientationSeconds = 0.05f;
        private Quaternion velocityOrientation;

        /// <summary>
        /// Gets or sets the orbit center transform.
        /// </summary>
        [IgnoreEvergine]
        public Transform3D CenterTransform { get; set; }

        /// <summary>
        /// Gets or sets orbit radius.
        /// </summary>
        public float Radius { get; set; } = 0.2f;

        /// <summary>
        /// Gets or sets distance multiplier used as threshold to update
        /// menu orientation.
        /// </summary>
        public float DistanceThreshold { get; set; } = 1.25f;

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            var camera = this.Managers.RenderManager.ActiveCamera3D;
            this.UpdatePosition(camera, this.Radius, gameTime);
            this.FaceTo(this.transform, camera.Position, gameTime);
        }

        private void UpdatePosition(Camera3D camera, float rad, TimeSpan gameTime)
        {
            if (this.CenterTransform == null)
            {
                return;
            }

            var from = this.CenterTransform.Position;
            var cameraDir = camera.Position - from;
            var distance = Vector3.Distance(this.CenterTransform.Position, camera.Position);

            cameraDir.Y = 0;
            cameraDir.Normalize();

            var offset = Vector3.Zero;
            var threshold = rad * this.DistanceThreshold;
            if (distance > threshold)
            {
                offset = cameraDir * rad;
                offset.Y -= rad * 0.5f;
            }
            else
            {
                offset = cameraDir * distance * 0.5f;
                offset.Y -= distance * 0.5f;
            }

            var to = from + offset;

            var toPosition = Vector3.SmoothDamp(this.transform.Position, to, ref this.velocity, this.dampVelocitySeconds, (float)gameTime.TotalSeconds);
            this.transform.Position = toPosition;
        }

        private void FaceTo(Transform3D trans, Vector3 cameraPosition, TimeSpan gameTime)
        {
            var posA = trans.Position;
            var posB = cameraPosition;
            var diff = posA - posB;

            // Apply a small lerp into the look at rotation;
            var from = trans.Orientation;
            trans.LookAt(posA + diff);
            var to = trans.Orientation;

            trans.Orientation = Quaternion.SmoothDamp(from, to, ref this.velocityOrientation, this.dampVelocityOrientationSeconds, (float)gameTime.TotalSeconds);
        }
    }
}
