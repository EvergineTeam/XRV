// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Components;
using Xrv.Core.Networking.Extensions;

namespace Xrv.Core.Networking.Properties.Session
{
    internal class SessionDataSynchronization : NetworkSerializablePropertySync<byte, SessionData>
    {
        public const byte NetworkingKey = 0x01;

        [BindService]
        private XrvService xrvService = null;

        public SessionDataSynchronization()
        {
            this.PropertyKeyByte = NetworkingKey;
            this.ProviderFilter = NetworkPropertyProviderFilter.Room;
        }

        internal void ForceSync() => this.UpdatePropertyValue();

        protected override void OnPropertyReadyToSet()
        {
            base.OnPropertyReadyToSet();

            var session = this.xrvService.Networking.Session;
            if (session.CurrentUserIsHost)
            {
                this.PropertyValue = new SessionData();
            }
            else
            {
                this.NotifySessionDataChange();
            }
        }

        protected override void OnPropertyAddedOrChanged() =>
            this.NotifySessionDataChange();

        protected override void OnPropertyRemoved()
        {
        }

        private void UpdatePropertyValue()
        {
            var session = this.xrvService.Networking.Session;
            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsPresenter)
            {
                Workarounds.ForceRefresh(this);
            }

            this.NotifySessionDataChange();
        }

        private void NotifySessionDataChange() =>
            this.xrvService.PubSub.Publish(new SessionDataSynchronizedMessage(this.PropertyValue));
    }
}
