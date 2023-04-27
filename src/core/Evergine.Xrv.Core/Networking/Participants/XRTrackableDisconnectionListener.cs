// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class XRTrackableDisconnectionListener : Component
    {
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, isRequired: false, isExactType: false)]
        private XRTrackableObserver observer = null;

        [BindComponent]
        private XRTrackableSynchronization synchronization = null;

        protected override void OnActivated()
        {
            base.OnActivated();
            this.NotifyDeviceTrackingLost();
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                if (this.observer != null)
                {
                    this.observer.AttachableStateChanged += this.XrDevice_AttachableStateChanged;
                }
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (this.observer != null)
            {
                this.observer.AttachableStateChanged -= this.XrDevice_AttachableStateChanged;
            }
        }

        private void XrDevice_AttachableStateChanged(object sender, AttachableObjectState state)
        {
            if (state == AttachableObjectState.Deactivated)
            {
                this.NotifyDeviceTrackingLost();
            }
        }

        private void NotifyDeviceTrackingLost()
        {
            if (this.observer != null)
            {
                this.observer.DeviceInfo.Pose = null;
                this.synchronization.SetPropertyValueWhenReady(this.observer.DeviceInfo);
            }
        }
    }
}
