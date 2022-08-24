using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Xrv.Core.Menu
{
    public class FollowPalmAnchor : Behavior
    {
        [BindComponent]
        private Transform3D transform3D = null;

        private Vector3 currentScaleVelocity;
        private Vector3 currentPositionVelocity;
        private Quaternion currentOrientationVelocity;

        private Transform3D target;

        public Transform3D Target
        {
            get => this.target;
            set => this.UpateTargetTransform(value);
        }

        public float SmoothTime { get; set; }

        private void UpateTargetTransform(Transform3D value)
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

        protected override void Update(TimeSpan gameTime)
        {
            if (this.SmoothTime <= 0)
            {
                return;
            }

            float dt = (float)gameTime.TotalSeconds;
            this.transform3D.Position = Vector3.SmoothDamp(this.transform3D.Position, this.Target.Position, ref this.currentPositionVelocity, this.SmoothTime, dt);
            this.transform3D.Orientation = Quaternion.SmoothDamp(this.transform3D.Orientation, this.Target.Orientation, ref this.currentOrientationVelocity, this.SmoothTime * 0.1f, dt);
            this.transform3D.Scale = Vector3.SmoothDamp(this.transform3D.Scale, this.Target.Scale, ref this.currentScaleVelocity, this.SmoothTime * 0.1f, dt);
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
