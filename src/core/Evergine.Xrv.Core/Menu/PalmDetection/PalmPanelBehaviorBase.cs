// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.XR;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.Menu.PalmDetection
{
    /// <summary>
    /// Palm twist behavior to notify when palm is detected and orientation changes.
    /// </summary>
    public abstract class PalmPanelBehaviorBase : Behavior
    {
        /// <summary>
        /// Whether the palm is currently up.
        /// </summary>
        protected bool isPalmUp;

        /// <summary>
        /// The total accumulated time since the last activation.
        /// </summary>
        protected float accumulatedTime;

        /// <summary>
        /// The owner's Transform3D.
        /// </summary>
        [BindComponent]
        protected Transform3D transform = null;

        /// <summary>
        /// Raised when active hand palm is twisted.
        /// </summary>
        public event EventHandler<bool> PalmUpChanged;

        /// <summary>
        /// Raised when active hand has changed.
        /// </summary>
        public event EventHandler<XRHandedness> ActiveHandednessChanged;

        /// <summary>
        /// Gets or sets distance from the hand to the entity.
        /// </summary>
        [RenderProperty(Tooltip = "Distance from the hand to the entity")]
        public float DistanceFromHand { get; set; } = 0.2f;

        /// <summary>
        /// Gets or sets amount that represents how much the palm has to be looking to the camera to consider it as up.
        /// </summary>
        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking to the camera to consider it as up", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraUpperThreshold { get; set; } = 0.8f;

        /// <summary>
        /// Gets or sets amount that represents how much the palm has to be looking away from the camera to consider it as down.
        /// </summary>
        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking away from the camera to consider it as down", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraLowerThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets amount that represents how much the hand has to be open to consider the palm as up.
        /// </summary>
        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be open to consider the palm as up", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmUpperThreshold { get; set; } = 0.8f;

        /// <summary>
        /// Gets or sets amount that represents how much the hand has to be open to consider the palm as down.
        /// </summary>
        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be closed to consider the palm as down", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmLowerThreshold { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets Minimum amount of time in seconds between two consecutive activation changes.
        /// </summary>
        [RenderProperty(Tooltip = "Minimum amount of time in seconds between two consecutive activation changes")]
        public float TimeBetweenActivationChanges { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets an explicit hand to be considered when detecting palm twist.
        /// </summary>
        [RenderProperty(Tooltip = "The desired handedness to consider by the component. Set to Undefined to consider both hands.")]
        public XRHandedness Handedness { get; set; } = XRHandedness.Undefined;

        /// <summary>
        /// Gets active hand.
        /// </summary>
        public abstract XRHandedness ActiveHandedness { get; }

        /// <summary>
        /// Gets a value indicating whether active palm is up.
        /// </summary>
        public bool IsPalmUp => this.isPalmUp;

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            this.accumulatedTime += (float)gameTime.TotalSeconds;

            // Get camera transform
            var cameraTransform = this.Managers.RenderManager?.ActiveCamera3D?.Transform;
            if (cameraTransform == null)
            {
                this.SetPalmUp(false);
            }

            this.Prepare();

            if (this.accumulatedTime > this.TimeBetweenActivationChanges)
            {
                var newPalmUp = this.GetNewPalmUp();
                this.SetPalmUp(newPalmUp);
            }

            var anchorPoint = this.GetAnchorPoint();

            if (anchorPoint != null)
            {
                // Calculate position offset
                var offset = this.ActiveHandedness == XRHandedness.LeftHand ? cameraTransform.WorldTransform.Right : cameraTransform.WorldTransform.Left;

                // Flatten offset vector
                offset.Y = 0;
                offset.Normalize();

                // Update positions
                var cameraPosition = cameraTransform.Position;

                var desiredPosition = anchorPoint.Value + (offset * this.DistanceFromHand);
                var desiredDirection = cameraPosition - desiredPosition;
                desiredDirection.Normalize();

                this.transform.LocalPosition = desiredPosition;
                this.transform.LookAt(this.transform.Position + desiredDirection);
            }
        }

        /// <summary>
        /// Invoke the ActiveHandednessChanged event.
        /// </summary>
        protected void OnActiveHandednessChanged()
        {
            this.ActiveHandednessChanged?.Invoke(this, this.ActiveHandedness);
        }

        /// <summary>
        /// Set the current palm up state.
        /// </summary>
        /// <param name="value">The new state.</param>
        protected void SetPalmUp(bool value)
        {
            if (this.isPalmUp != value)
            {
                this.isPalmUp = value;
                this.PalmUpChanged?.Invoke(this, value);

                this.accumulatedTime = 0;
            }
        }

        /// <summary>
        /// Read variables and prepare for palm state change.
        /// </summary>
        protected abstract void Prepare();

        /// <summary>
        /// Get the new palm up state.
        /// </summary>
        /// <returns>The new palm state.</returns>
        protected abstract bool GetNewPalmUp();

        /// <summary>
        /// Get the point where the panel will be anchored to.
        /// </summary>
        /// <returns>The anchor point, null if none.</returns>
        protected abstract Vector3? GetAnchorPoint();
    }
}
