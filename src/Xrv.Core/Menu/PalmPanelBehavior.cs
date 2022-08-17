using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.Framework.XR.TrackedDevices;
using Evergine.Mathematics;
using System;

namespace Xrv.Core.Menu
{
    public class PalmPanelBehavior : Behavior, IPalmPanelBehavior
    {
        private XRTrackedDevice lastTrackedDevice;

        private bool isPalmUp;
        private float accumulatedTime;

        [BindService]
        protected XRPlatform xrPlatform;

        [BindComponent]
        protected Transform3D transform;

        [RenderProperty(Tooltip = "Distance from the hand to the entity")]
        public float DistanceFromHand { get; set; } = 0.2f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking to the camera to consider it as up", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraUpperThreshold { get; set; } = 0.8f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking away from the camera to consider it as down", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraLowerThreshold { get; set; } = 0.7f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be open to consider the palm as up", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmUpperThreshold { get; set; } = 0.8f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be closed to consider the palm as down", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmLowerThreshold { get; set; } = 0.7f;

        [RenderProperty(Tooltip = "Minimum amount of time in seconds between two consecutive activation changes")]
        public float TimeBetweenActivationChanges { get; set; } = 0.5f;

        [RenderProperty(Tooltip = "The desired handedness to consider by the component. Set to Undefined to consider both hands.")]
        public XRHandedness Handedness { get; set; } = XRHandedness.Undefined;

        public XRHandedness ActiveHandedness => this.lastTrackedDevice?.Handedness ?? XRHandedness.Undefined;

        public bool IsPalmUp => this.isPalmUp;

        public event EventHandler<bool> PalmUpChanged;

        public event EventHandler<XRHandedness> ActiveHandednessChanged;

        protected override void Update(TimeSpan gameTime)
        {
            this.accumulatedTime += (float)gameTime.TotalSeconds;

            // Get camera transform
            var cameraTransform = this.Managers.RenderManager?.ActiveCamera3D?.Transform;
            if (cameraTransform == null)
            {
                this.SetPalmUp(false);
            }

            if (this.accumulatedTime > this.TimeBetweenActivationChanges)
            {
                this.SetPalmUp(this.TryGetValidPalm(cameraTransform));
            }

            if (this.lastTrackedDevice == null ||
                !this.lastTrackedDevice.TryGetArticulatedHandJoint(XRHandJointKind.MiddleProximal, out var middleProximalJoint))
            {
                return;
            }

            // Calculate position offset
            var offset = this.lastTrackedDevice.Handedness == XRHandedness.LeftHand ? cameraTransform.WorldTransform.Right : cameraTransform.WorldTransform.Left;

            // Flatten offset vector
            offset.Y = 0;
            offset.Normalize();

            // Update positions
            var cameraPosition = cameraTransform.Position;

            var desiredPosition = middleProximalJoint.Pose.Position + offset * this.DistanceFromHand;
            var desiredDirection = cameraPosition - desiredPosition;
            desiredDirection.Normalize();

            this.transform.Position = desiredPosition;
            this.transform.LookAt(desiredPosition + desiredDirection);
        }

        private bool TryGetValidPalm(Transform3D cameraTransform)
        {
            if (this.Handedness == XRHandedness.Undefined)
            {
                var handedness = this.lastTrackedDevice?.Handedness ?? XRHandedness.LeftHand;
                if (this.TryGetValidPalm(cameraTransform, handedness))
                {
                    return true;
                }

                handedness = handedness == XRHandedness.LeftHand ? XRHandedness.RightHand : XRHandedness.LeftHand;
                return this.TryGetValidPalm(cameraTransform, handedness);
            }
            else
            {
                return this.TryGetValidPalm(cameraTransform, this.Handedness);
            }
        }

        private bool TryGetValidPalm(Transform3D cameraTransform, XRHandedness handedness)
        {
            // Get tracker device
            var trackedDevice = this.xrPlatform.InputTracking?.GetDeviceByTypeAndHandedness(XRTrackedDeviceType.Hand, handedness);
            if (trackedDevice == null)
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

            if (!allVectorsOverUpperThreshold || anyVectorUnderLowerThreshold)
            {
                return false;
            }

            var previousHandedness = this.ActiveHandedness;
            this.lastTrackedDevice = trackedDevice;

            if (this.ActiveHandedness != previousHandedness)
            {
                this.ActiveHandednessChanged?.Invoke(this, this.ActiveHandedness);
            }

            return true;
        }

        private void SetPalmUp(bool value)
        {
            if (this.isPalmUp != value)
            {
                this.isPalmUp = value;
                this.PalmUpChanged?.Invoke(this, value);

                this.accumulatedTime = 0;
            }
        }
    }
}
