// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.Framework.XR.TrackedDevices;
using Evergine.Mathematics;

namespace Evergine.Xrv.Core.Menu.PalmDetection
{
    /// <inheritdoc/>
    public class XRPalmPanelBehavior : PalmPanelBehaviorBase
    {
        [BindService(isRequired: false)]
        private XRPlatform xrPlatform = null;

        private XRTrackedDevice lastTrackedDevice;

        /// <inheritdoc/>
        public override XRHandedness ActiveHandedness => this.lastTrackedDevice?.Handedness ?? XRHandedness.Undefined;

        /// <inheritdoc/>
        protected override void Prepare()
        {
        }

        /// <inheritdoc/>
        protected override bool GetNewPalmUp()
        {
            if (this.Handedness == XRHandedness.Undefined)
            {
                var handedness = this.lastTrackedDevice?.Handedness ?? XRHandedness.LeftHand;
                if (this.TryGetValidPalm(handedness))
                {
                    return true;
                }

                handedness = handedness == XRHandedness.LeftHand ? XRHandedness.RightHand : XRHandedness.LeftHand;
                return this.TryGetValidPalm(handedness);
            }
            else
            {
                return this.TryGetValidPalm(this.Handedness);
            }
        }

        /// <inheritdoc/>
        protected override Vector3? GetAnchorPoint()
        {
            if (this.lastTrackedDevice == null ||
                !this.lastTrackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.MiddleProximal, out var middleProximalJoint))
            {
                return null;
            }

            return middleProximalJoint.Pose.Position;
        }

        private bool TryGetValidPalm(XRHandedness handedness)
        {
            var cameraTransform = this.Managers.RenderManager?.ActiveCamera3D?.Transform;

            // Get tracker device
            var trackedDevice = this.xrPlatform?.InputTracking?.GetDeviceByTypeAndHandedness(XRTrackedDeviceType.Hand, handedness);
            if (trackedDevice == null || !trackedDevice.PoseIsValid)
            {
                return false;
            }

            // Get joints status
            if (!trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.MiddleMetacarpal, out var middleMetacarpalJoint) ||
                !trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.IndexTip, out var indexTipJoint) ||
                !trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.RingTip, out var ringTipJoint))
            {
                return false;
            }

            if (this.accumulatedTime < this.TimeBetweenActivationChanges)
            {
                return this.isPalmUp;
            }

            // Get the positions for the joints that will be used to determine if the palm is open
            var middleMetacarpalPosition = middleMetacarpalJoint.Pose.Position;
            var middleMetacarpalOrientation = middleMetacarpalJoint.Pose.Orientation;
            var indexTipPosition = indexTipJoint.Pose.Position;
            var ringTipPosition = ringTipJoint.Pose.Position;

            // Calculate hand plane
            var handPlane = trackedDevice.Handedness == XRHandedness.LeftHand ?
                            new Plane(middleMetacarpalPosition, ringTipPosition, indexTipPosition) :
                            new Plane(middleMetacarpalPosition, indexTipPosition, ringTipPosition);

            // Check that the palm is looking at the camera and the hand is open
            var palmNormal = middleMetacarpalOrientation * Vector3.Down;
            var fingersNormal = handPlane.Normal;
            var cameraNormal = Vector3.Normalize(cameraTransform.Position - middleMetacarpalPosition);

            var cameraPalm = Vector3.Dot(cameraNormal, palmNormal);
            var fingersPalm = Vector3.Dot(fingersNormal, palmNormal);
            var allVectorsOverUpperThreshold = cameraPalm > this.LookAtCameraUpperThreshold && fingersPalm > this.OpenPalmUpperThreshold;
            var anyVectorUnderLowerThreshold = cameraPalm < this.LookAtCameraLowerThreshold || fingersPalm < this.OpenPalmLowerThreshold;

            if (this.Managers.RenderManager.DebugLines)
            {
                var lineBatch = this.Managers.RenderManager.LineBatch3D;
                trackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.Palm, out var palm);
                lineBatch.DrawPoint(middleMetacarpalPosition, 0.01f, Color.Red);
                lineBatch.DrawPoint(indexTipPosition, 0.01f, Color.Green);
                lineBatch.DrawPoint(ringTipPosition, 0.01f, Color.Blue);
                lineBatch.DrawRay(palm.Pose.Position, palmNormal * 0.02f, Color.Cyan);
                lineBatch.DrawRay(palm.Pose.Position, cameraNormal * 0.02f, Color.Magenta);
                lineBatch.DrawRay(palm.Pose.Position, fingersNormal * 0.02f, Color.Yellow);
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
    }
}
