// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.XR;
using Evergine.Framework;
using System;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class XRTrackableControllerObserver : XRTrackableObserver
    {
        [BindComponent]
        private TrackXRController xrController = null;

        protected override void Update(TimeSpan gameTime)
        {
            var state = this.xrController.ControllerState;
            this.UpdatePoseInfo(this.xrController.LocalPose, state.IsConnected);
        }
    }
}
