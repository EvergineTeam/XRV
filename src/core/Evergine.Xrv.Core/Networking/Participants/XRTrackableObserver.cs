// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.XR;
using Evergine.Mathematics;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal abstract class XRTrackableObserver : Behavior
    {
        [BindComponent(source: BindComponentSource.ParentsSkipOwner)]
        private Transform3D parentTransform = null;

        [BindComponent(source: BindComponentSource.ParentsSkipOwner)]
        private XRTrackableSynchronization propertySync = null;

        private XRDeviceInfo deviceInfo;
        private XRHandedness handedness;

        protected XRTrackableObserver()
        {
            this.deviceInfo = new XRDeviceInfo();
        }

        public XRDeviceInfo DeviceInfo { get => this.deviceInfo; }

        public XRHandedness Handedness
        {
            get => this.handedness;
            set
            {
                if (this.handedness != value)
                {
                    this.handedness = value;
                    this.deviceInfo.Handedness = value;
                }
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.deviceInfo.Pose = null;
        }

        protected virtual void UpdatePoseInfo(ViewPose pose, bool succeded)
        {
            if (succeded)
            {
                var poseTransform = Matrix4x4.CreateFromTRS(pose.Position, pose.Orientation, Vector3.One);
                this.deviceInfo.Pose = poseTransform * this.parentTransform.WorldInverseTransform;
            }
            else
            {
                this.deviceInfo.Pose = null;
            }

            this.propertySync.SetPropertyValueWhenReady(this.deviceInfo);
        }
    }
}
