// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Networking.Client;
using Evergine.Networking.Components;
using System.Linq;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class XRTrackableSynchronization : NetworkSerializablePropertySync<byte, XRDeviceInfo>
    {
        [BindService]
        private MatchmakingClientService client = null;

        [BindComponent(source: BindComponentSource.Parents)]
        private NetworkPlayerProvider networkingProvider = null;

        [BindComponent]
        private Transform3D transform = null;

        private XRDeviceInfo tmpDeviceInfo;

        public void SetPropertyValueWhenReady(XRDeviceInfo deviceInfo)
        {
            if (this.IsReady)
            {
                this.PropertyValue = deviceInfo;
            }
            else
            {
                this.tmpDeviceInfo = deviceInfo;
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.tmpDeviceInfo = null;
        }

        protected override void OnPropertyReadyToSet()
        {
            base.OnPropertyReadyToSet();
            if (this.tmpDeviceInfo != null && this.IsLocalPlayerData())
            {
                this.PropertyValue = this.tmpDeviceInfo;
                this.tmpDeviceInfo = null;
            }
        }

        protected override void OnPropertyAddedOrChanged()
        {
            if (this.IsLocalPlayerData())
            {
                return;
            }

            var data = this.PropertyValue;
            if (data == null)
            {
                return;
            }

            if (data.Pose.HasValue)
            {
                this.transform.LocalTransform = data.Pose.Value;
            }

            this.Owner.ChildEntities.First().IsEnabled = data.Pose.HasValue;
        }

        protected override void OnPropertyRemoved()
        {
        }

        private bool IsLocalPlayerData() =>
            this.client.LocalPlayer.Id == this.networkingProvider.PlayerId;
    }
}
