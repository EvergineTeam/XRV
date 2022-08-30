// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Xrv.Core.Menu
{
    /// <summary>
    /// Behavior to make menu to follow user palm.
    /// </summary>
    public class FollowPalmAnchor : Behavior
    {
        [BindComponent]
        private Transform3D transform3D = null;

        private Vector3 currentScaleVelocity;
        private Vector3 currentPositionVelocity;
        private Quaternion currentOrientationVelocity;

        private Transform3D target;

        /// <summary>
        /// Gets or sets target transform.
        /// </summary>
        public Transform3D Target
        {
            get => this.target;
            set => this.UpdateTargetTransform(value);
        }

        /// <summary>
        /// Gets or sets target offset.
        /// </summary>
        public Vector3 TargetOffset { get; set; } = new Vector3(-0.016f, 0.05f, 0);

        /// <summary>
        /// Gets or sets smooth time.
        /// </summary>
        public float SmoothTime { get; set; }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.SmoothTime <= 0)
            {
                return;
            }

            float dt = (float)gameTime.TotalSeconds;

            this.transform3D.Position = Vector3.SmoothDamp(this.transform3D.Position, this.Target.Position + this.TargetOffset, ref this.currentPositionVelocity, this.SmoothTime, dt);
            this.transform3D.Orientation = Quaternion.SmoothDamp(this.transform3D.Orientation, this.Target.Orientation, ref this.currentOrientationVelocity, this.SmoothTime * 0.1f, dt);
            this.transform3D.Scale = Vector3.SmoothDamp(this.transform3D.Scale, this.Target.Scale, ref this.currentScaleVelocity, this.SmoothTime * 0.1f, dt);
        }

        private void UpdateTargetTransform(Transform3D value)
        {
            if (this.target != value)
            {
                if (this.target != null)
                {
                    this.target.TransformChanged -= this.Target_TransformChanged;
                }

                this.target = value;
                this.IsEnabled = false;

                if (this.target != null)
                {
                    this.target.TransformChanged += this.Target_TransformChanged;
                    this.IsEnabled = this.SmoothTime >= 0;
                    this.Target_TransformChanged(this, EventArgs.Empty);
                }
            }
        }

        private void Target_TransformChanged(object sender, EventArgs args)
        {
            if (this.SmoothTime <= 0)
            {
                this.transform3D.WorldTransform = this.Target.WorldTransform;
            }
        }
    }
}
