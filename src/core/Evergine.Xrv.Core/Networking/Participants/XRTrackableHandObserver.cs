// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.XR;
using Evergine.Framework;
using Evergine.Framework.XR;
using System;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class XRTrackableHandObserver : XRTrackableObserver
    {
        [BindComponent]
        private TrackXRArticulatedHand xrHand = null;

        protected override void Update(TimeSpan gameTime)
        {
            if (this.xrHand.SupportedHandJointKind == null)
            {
                return;
            }

            bool succeeded = this.xrHand.TryGetArticulatedHandJointLocal(XRHandJointKind.Palm, out XRHandJoint palm);
            this.UpdatePoseInfo(palm.Pose, succeeded);
        }
    }
}
