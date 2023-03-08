// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Components;

namespace Evergine.Xrv.Core.Networking.Properties.Session
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

        // We use this cached value to avoid problems while reading-writing property values
        // from different threads.
        public SessionData CurrentValue { get; private set; }

        internal void SetData(SessionData sessionData)
        {
            this.CurrentValue = sessionData;
            this.PropertyValue = sessionData;
            this.NotifySessionDataChange();
        }

        internal void NotifySessionDataChange() =>
            this.xrvService.Services.Messaging.Publish(new SessionDataSynchronizedMessage(this.PropertyValue));

        protected override void OnPropertyReadyToSet()
        {
            base.OnPropertyReadyToSet();

            var session = this.xrvService.Networking.Session;
            if (session.CurrentUserIsHost)
            {
                this.CurrentValue = new SessionData();
                this.PropertyValue = this.CurrentValue;
            }
            else
            {
                this.NotifySessionDataChange();
            }
        }

        protected override void OnPropertyAddedOrChanged()
        {
            this.CurrentValue = this.PropertyValue;
            this.NotifySessionDataChange();
        }

        protected override void OnPropertyRemoved()
        {
        }
    }
}
