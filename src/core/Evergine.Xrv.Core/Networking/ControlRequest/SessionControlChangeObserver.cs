// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;
using Evergine.Xrv.Core.Services.Messaging;

namespace Evergine.Xrv.Core.Networking.ControlRequest
{
    /// <summary>
    /// Listens to changes over session control, and lets you to add custom
    /// logic for that for extending classes.
    /// </summary>
    public abstract class SessionControlChangeObserver : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private PubSub pubSub = null;
        private Guid presenterSubscription;
        private Guid sessionStateSubscription;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.pubSub = this.xrvService.Services.Messaging;
                this.presenterSubscription = this.pubSub.Subscribe<SessionPresenterUpdatedMessage>(this.OnPresenterChanged);
                this.sessionStateSubscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStateChanged);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.presenterSubscription);
            this.pubSub.Unsubscribe(this.sessionStateSubscription);
        }

        /// <summary>
        /// Invoked when session control is gained.
        /// </summary>
        protected virtual void OnControlGained()
        {
        }

        /// <summary>
        /// Invoked when session control is lost.
        /// </summary>
        protected virtual void OnControlLost()
        {
        }

        private void OnPresenterChanged(SessionPresenterUpdatedMessage message)
        {
            if (message.CurrentIsPresenter)
            {
                this.OnControlGained();
            }
            else
            {
                this.OnControlLost();
            }
        }

        private void OnSessionStateChanged(SessionStatusChangeMessage message)
        {
            if (message.NewStatus == SessionStatus.Disconnected)
            {
                this.OnControlGained();
            }
        }
    }
}
