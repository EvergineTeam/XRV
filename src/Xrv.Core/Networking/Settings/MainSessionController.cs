// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;
using Xrv.Core.Messaging;

namespace Xrv.Core.Networking.Settings
{
    /// <summary>
    /// Main controller for sessions panel.
    /// </summary>
    public class MainSessionController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private CreateOrJoinSessionController createOrJoin = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private CreatingOrJoiningSessionController creatingOrJoining = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private LeaveSessionController leaveSession = null;

        private Guid subscription;
        private PubSub pubSub = null;
        private NetworkSystem networkSystem;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.networkSystem = this.xrvService.Networking;
                this.pubSub = this.xrvService.PubSub;
                this.subscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateSessionStatus(this.networkSystem.Session.Status);
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub?.Unsubscribe(this.subscription);
        }

        private void OnSessionStatusChange(SessionStatusChangeMessage message) =>
            this.UpdateSessionStatus(message.NewStatus);

        private void UpdateSessionStatus(SessionStatus currentStatus)
        {
            this.createOrJoin.Owner.IsEnabled = currentStatus == SessionStatus.Disconnected;
            this.creatingOrJoining.Owner.IsEnabled = currentStatus == SessionStatus.Joining;
            this.leaveSession.Owner.IsEnabled = currentStatus == SessionStatus.Joined;
        }
    }
}
