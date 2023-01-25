// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;
using Xrv.Core.Services.Messaging;
using Xrv.Core.UI.Buttons;

namespace Xrv.Core.Networking.ControlRequest
{
    internal class HandMenuButtonStateUpdater : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private VisuallyEnabledController visuallyEnabled = null;

        private NetworkSystem networking = null;
        private PubSub pubSub = null;
        private Guid subscription;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.networking = this.xrvService.Networking;
                this.pubSub = this.xrvService.Services.Messaging;
                this.subscription = this.pubSub.Subscribe<SessionPresenterUpdatedMessage>(this.OnPresenterUpdated);
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.subscription);
        }

        private void OnPresenterUpdated(SessionPresenterUpdatedMessage message)
        {
            bool enable = this.networking.Session.CurrentUserIsHost || !message.CurrentIsPresenter;
            this.visuallyEnabled.IsVisuallyEnabled = enable;
        }
    }
}
