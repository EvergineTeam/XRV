// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.Framework.XR.TrackedDevices;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.Menu.PalmDetection
{
    /// <inheritdoc/>
    public class XRPalmPanelBehavior : PalmPanelBehaviorBase
    {
        [BindService(isRequired: false)]
        private XRPlatform xrPlatform = null;

        [BindComponent(source: BindComponentSource.ParentsSkipOwner)]
        private Transform3D parentTransform3D = null;

        private XRTrackedDevice lastTrackedDevice;
        private bool lastButtonState;
        private bool menuToggleState;

        /// <inheritdoc/>
        public override XRHandedness ActiveHandedness => this.lastTrackedDevice?.Handedness ?? XRHandedness.Undefined;

        /// <summary>
        /// Gets or sets the button that will toggle the palm panel when a controller is detected instead of a hand.
        /// </summary>
        [RenderProperty(Tooltip = "The button that will toggle the palm panel when a controller is detected instead of a hand.")]
        public XRButtons ControllerMenuButton { get; set; } = XRButtons.Button2;

        /// <inheritdoc/>
        protected override void Prepare()
        {
        }

        /// <inheritdoc/>
        protected override bool GetNewPalmUp()
        {
            return this.TryGetNewPalmUpBy(this.TryGetValidPalm) || this.TryGetNewPalmUpBy(this.TryGetValidControllerMenu);
        }

        /// <inheritdoc/>
        protected override Vector3? GetAnchorPoint()
        {
            if (this.lastTrackedDevice != null && this.lastTrackedDevice.PoseIsValid)
            {
                if (this.lastTrackedDevice.DeviceType == XRTrackedDeviceType.Hand)
                {
                    if (this.lastTrackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.MiddleProximal, out var middleProximalJoint))
                    {
                        return Vector3.Transform(middleProximalJoint.Pose.Position, this.parentTransform3D.WorldTransform);
                    }
                }
                else if (this.lastTrackedDevice.DeviceType == XRTrackedDeviceType.Controller)
                {
                    if (this.lastTrackedDevice.GetTrackingState(out var trackingState))
                    {
                        return Vector3.Transform(trackingState.Pose.Position, this.parentTransform3D.WorldTransform);
                    }
                }
            }

            return null;
        }

        private bool TryGetNewPalmUpBy(Func<XRHandedness, bool> tryGetValidStateMethod)
        {
            if (this.Handedness == XRHandedness.Undefined)
            {
                var handedness = this.lastTrackedDevice?.Handedness ?? XRHandedness.LeftHand;
                if (tryGetValidStateMethod(handedness))
                {
                    return true;
                }

                handedness = handedness == XRHandedness.LeftHand ? XRHandedness.RightHand : XRHandedness.LeftHand;
                return tryGetValidStateMethod(handedness);
            }
            else
            {
                return tryGetValidStateMethod(this.Handedness);
            }
        }

        private bool TryGetValidPalm(XRHandedness handedness)
        {
            // Get tracked device
            if (!this.TryGetDeviceByTypeAndHandednessThrottled(XRTrackedDeviceType.Hand, handedness, out var trackedDevice, out var menuResult))
            {
                return menuResult;
            }

            // Get joints status
            if (!trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.MiddleMetacarpal, out var middleMetacarpalJoint) ||
                !trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.IndexTip, out var indexTipJoint) ||
                !trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.RingTip, out var ringTipJoint))
            {
                return false;
            }

            // Get the positions for the joints that will be used to determine if the palm is open
            var middleMetacarpalPosition = Vector3.Transform(middleMetacarpalJoint.Pose.Position, this.parentTransform3D.WorldTransform);
            var middleMetacarpalOrientation = middleMetacarpalJoint.Pose.Orientation * this.parentTransform3D.WorldTransform.Orientation;
            var indexTipPosition = Vector3.Transform(indexTipJoint.Pose.Position, this.parentTransform3D.WorldTransform);
            var ringTipPosition = Vector3.Transform(ringTipJoint.Pose.Position, this.parentTransform3D.WorldTransform);

            // Calculate hand plane
            var handPlane = trackedDevice.Handedness == XRHandedness.LeftHand ?
                            new Plane(middleMetacarpalPosition, ringTipPosition, indexTipPosition) :
                            new Plane(middleMetacarpalPosition, indexTipPosition, ringTipPosition);

            // Check that the palm is looking at the camera and the hand is open
            var palmNormal = middleMetacarpalOrientation * Vector3.Down;
            var fingersNormal = handPlane.Normal;

            var cameraTransform = this.Managers.RenderManager?.ActiveCamera3D?.Transform;
            var cameraNormal = Vector3.Normalize(cameraTransform.Position - middleMetacarpalPosition);

            var cameraPalm = Vector3.Dot(cameraNormal, palmNormal);
            var fingersPalm = Vector3.Dot(fingersNormal, palmNormal);
            var allVectorsOverUpperThreshold = cameraPalm > this.LookAtCameraUpperThreshold && fingersPalm > this.OpenPalmUpperThreshold;
            var anyVectorUnderLowerThreshold = cameraPalm < this.LookAtCameraLowerThreshold || fingersPalm < this.OpenPalmLowerThreshold;

            if (this.Managers.RenderManager.DebugLines)
            {
                trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.Palm, out var palm);

                var lineBatch = this.Managers.RenderManager.LineBatch3D;

                lineBatch.DrawPoint(middleMetacarpalPosition, 0.01f, Color.Red);
                lineBatch.DrawPoint(indexTipPosition, 0.01f, Color.Green);
                lineBatch.DrawPoint(ringTipPosition, 0.01f, Color.Blue);

                var palmPosition = Vector3.Transform(palm.Pose.Position, this.parentTransform3D.WorldTransform);

                lineBatch.DrawRay(palmPosition, palmNormal * 0.02f, Color.Cyan);
                lineBatch.DrawRay(palmPosition, cameraNormal * 0.02f, Color.Magenta);
                lineBatch.DrawRay(palmPosition, fingersNormal * 0.02f, Color.Yellow);
            }

            if ((!this.IsPalmUp && !allVectorsOverUpperThreshold) || (this.IsPalmUp && anyVectorUnderLowerThreshold))
            {
                return false;
            }

            var previousHandedness = this.ActiveHandedness;
            this.lastTrackedDevice = trackedDevice;

            if (this.ActiveHandedness != previousHandedness)
            {
                this.OnActiveHandednessChanged();
            }

            return true;
        }

        private bool TryGetValidControllerMenu(XRHandedness handedness)
        {
            // Get tracked device
            if (!this.TryGetDeviceByTypeAndHandednessThrottled(XRTrackedDeviceType.Controller, handedness, out var trackedDevice, out var menuResult))
            {
                return menuResult;
            }

            if (trackedDevice.GetControllerState(out var controllerGenericState))
            {
                var newButtonState = controllerGenericState.IsButtonPressed(this.ControllerMenuButton);
                this.menuToggleState ^= newButtonState && !this.lastButtonState;
                this.lastButtonState = newButtonState;
            }

            if (this.menuToggleState)
            {
                var previousHandedness = this.ActiveHandedness;
                this.lastTrackedDevice = trackedDevice;

                if (this.ActiveHandedness != previousHandedness)
                {
                    this.OnActiveHandednessChanged();
                }
            }

            return this.menuToggleState;
        }

        private bool TryGetDeviceByTypeAndHandednessThrottled(XRTrackedDeviceType type, XRHandedness handedness, out XRTrackedDevice trackedDevice, out bool menuResult)
        {
            menuResult = false;

            trackedDevice = this.xrPlatform?.InputTracking?.GetDeviceByTypeAndHandedness(type, handedness);
            if (trackedDevice == null || !trackedDevice.PoseIsValid)
            {
                return false;
            }

            if (this.accumulatedTime < this.TimeBetweenActivationChanges)
            {
                menuResult = this.isPalmUp;
                return false;
            }

            return true;
        }
    }
}
