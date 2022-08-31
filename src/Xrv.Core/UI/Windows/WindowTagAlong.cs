// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Attributes.Converters;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System;

namespace Xrv.Core.UI.Windows
{
    /// <summary>
    /// Tag-along for windows.
    /// </summary>
    public class WindowTagAlong : Behavior
    {
        private Vector3 desiredPosition;
        private Vector3 lastCameraToPanelVector;
        private Quaternion desiredOrientation = Quaternion.Identity;

        [BindComponent]
        private Transform3D transform = null;

        /// <summary>
        /// Gets or sets the max horizontal angle of the panel [0-180].
        /// </summary>
        [RenderProperty(typeof(FloatRadianToDegreeConverter))]
        public float MaxHAngle { get; set; }

        /// <summary>
        /// Gets or sets the max vertical angle of the panel [0-180].
        /// </summary>
        [RenderProperty(typeof(FloatRadianToDegreeConverter))]
        public float MaxVAngle { get; set; }

        /// <summary>
        /// Gets or sets the max angle allowed between camera forward and panel forward [0-180].
        /// </summary>
        [RenderProperty(typeof(FloatRadianToDegreeConverter))]
        public float MaxLookAtAngle { get; set; } = MathHelper.ToRadians(15);

        /// <summary>
        /// Gets or sets the minimum distance.
        /// </summary>
        public float MinDistance { get; set; } = 0.4f;

        /// <summary>
        /// Gets or sets the maximum distance.
        /// </summary>
        public float MaxDistance { get; set; } = 1f;

        /// <summary>
        /// Gets or sets a value indicating whether vertical lookAt is disabled, so it only rotates on XZ plane.
        /// </summary>
        public bool DisableVerticalLookAt { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum vertical distance from camera.
        /// </summary>
        public float MaxVerticalDistance { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the smooth factor for the position.
        /// </summary>
        public float SmoothPositionFactor { get; set; } = 0.1f;

        /// <summary>
        /// Gets or sets the smooth factor for the orientation.
        /// </summary>
        public float SmoothOrientationFactor { get; set; } = 0.05f;

        /// <summary>
        /// Gets or sets the smooth factor for the distance.
        /// </summary>
        public float SmoothDistanceFactor { get; set; } = 0.5f;

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            // Gets the camera properties
            var renderManager = this.Managers.RenderManager;
            var cameraTransform = renderManager.ActiveCamera3D?.Transform;
            if (cameraTransform == null)
            {
                return;
            }

            var cameraPosition = cameraTransform.Position;
            var cameraForward = cameraTransform.WorldTransform.Forward;

            // Get the panel properties
            var panelPosition = this.transform.Position;

            var cameraToPanelVector = panelPosition - cameraPosition;
            var cameraPositionXZ = cameraPosition;
            cameraPositionXZ.Y = 0;
            var panelDistance = cameraToPanelVector.Length();

            // Compute horizontal angle to the camera projection on XZ plane
            var cameraForwardH = new Vector3(cameraForward.X, 0, cameraForward.Z);
            var panelDirectionH = new Vector3(cameraToPanelVector.X, 0, cameraToPanelVector.Z);
            float panelAngleH = Vector3.Angle(cameraForwardH, panelDirectionH);
            var maxHAngleReached = (panelAngleH - this.MaxHAngle) > 0.001f;
            if (maxHAngleReached)
            {
                panelDirectionH = Vector3.Lerp(cameraForwardH, panelDirectionH, this.MaxHAngle / panelAngleH);
            }

            // Compute vertical angle to the camera projection on YZ plane or XY plane
            var projectOverYZ = Math.Abs(cameraForward.X) <= Math.Abs(cameraForward.Z); // if false, project over XY
            var cameraForwardV = projectOverYZ ? new Vector3(0, cameraForward.Y, cameraForward.Z) : new Vector3(cameraForward.X, cameraForward.Y, 0);
            var panelDirectionV = projectOverYZ ? new Vector3(0, cameraToPanelVector.Y, cameraToPanelVector.Z) : new Vector3(cameraToPanelVector.X, cameraToPanelVector.Y, 0);
            float panelAngleV = Vector3.Angle(cameraForwardV, panelDirectionV);
            if ((panelAngleV - this.MaxVAngle) > 0.001f)
            {
                panelDirectionV = Vector3.Lerp(cameraForwardV, panelDirectionV, this.MaxVAngle / panelAngleV);
            }

            // Compose final panel direction
            cameraToPanelVector.X = projectOverYZ ? panelDirectionH.X : (panelDirectionH.X + panelDirectionV.X) / 2f;
            cameraToPanelVector.Y = panelDirectionV.Y;
            cameraToPanelVector.Z = projectOverYZ ? (panelDirectionH.Z + panelDirectionV.Z) / 2f : panelDirectionH.Z;

            // Prevent window shaking
            if (Math.Abs(this.lastCameraToPanelVector.Length() - cameraToPanelVector.Length()) > 0.001f)
            {
                this.lastCameraToPanelVector = cameraToPanelVector;
            }
            else
            {
                cameraToPanelVector = this.lastCameraToPanelVector;
            }

            cameraToPanelVector.Normalize();

            // Calculate collisions
            var finalPosition = MathHelper.Clamp(panelDistance, this.MinDistance, this.MaxDistance);

            // Compute distance
            var desiredDistance = MathHelper.Lerp(panelDistance, finalPosition, this.SmoothDistanceFactor);
            var desiredCameraToPanel = cameraToPanelVector * desiredDistance;
            desiredCameraToPanel.Y = MathHelper.Clamp(desiredCameraToPanel.Y, -this.MaxVerticalDistance, this.MaxVerticalDistance);
            this.desiredPosition = cameraPosition + desiredCameraToPanel;

            var forwardsAngle = Vector3.Angle(cameraForward, this.transform.WorldTransform.Forward);
            if ((forwardsAngle - this.MaxLookAtAngle) > 0.001f)
            {
                // We multiply dest by -1 to invert look at, as it's turning window backwards
                var dest = (this.desiredPosition - (this.DisableVerticalLookAt ? cameraPositionXZ : cameraPosition)) * -1;
                var up = Vector3.Up;
                Quaternion.CreateFromLookAt(ref dest, ref up, out this.desiredOrientation);
            }

            if (renderManager.DebugLines)
            {
                renderManager.LineBatch3D.DrawForward(this.transform.WorldTransform, 0.1f);
            }

            // Sets final values
            this.transform.Position = Vector3.Lerp(this.transform.Position, this.desiredPosition, this.SmoothPositionFactor);
            this.transform.Orientation = Quaternion.Lerp(this.transform.Orientation, this.desiredOrientation, this.SmoothOrientationFactor);
        }
    }
}
