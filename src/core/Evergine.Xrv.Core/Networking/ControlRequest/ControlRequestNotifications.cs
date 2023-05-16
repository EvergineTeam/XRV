// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Client;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Services.Messaging;
using Evergine.Xrv.Core.UI.Windows;
using System;

namespace Evergine.Xrv.Core.Networking.ControlRequest
{
    internal class ControlRequestNotifications : SessionControlChangeObserver
    {
        [BindService]
        private XrvService xrv = null;

        [BindService]
        private LocalizationService localization = null;

        [BindService]
        private MatchmakingClientService client = null;

        private WindowsSystem windows;
        private PubSub pubSub;
        private Guid sessionStatusSubscription;
        private bool justJoinedToTheRoom;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.windows = this.xrv.WindowsSystem;
                this.pubSub = this.xrv.Services.Messaging;
                this.sessionStatusSubscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub?.Unsubscribe(this.sessionStatusSubscription);
        }

        protected override void OnControlGained()
        {
            base.OnControlGained();

            if (!this.client.IsConnected)
            {
                return;
            }

            if (this.justJoinedToTheRoom)
            {
                this.justJoinedToTheRoom = false;
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Control_Notifications_Gained_Title),
                this.localization.GetString(() => Resources.Strings.Sessions_Control_Notifications_Gained_Text));
        }

        protected override void OnControlLost()
        {
            base.OnControlLost();

            if (!this.client.IsConnected)
            {
                return;
            }

            if (this.justJoinedToTheRoom)
            {
                this.justJoinedToTheRoom = false;
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Control_Notifications_Lost_Title),
                this.localization.GetString(() => Resources.Strings.Sessions_Control_Notifications_Lost_Text));
        }

        private void OnSessionStatusChange(SessionStatusChangeMessage change)
        {
            if (change.NewStatus == SessionStatus.Joining)
            {
                this.justJoinedToTheRoom = true;
            }
        }
    }
}
