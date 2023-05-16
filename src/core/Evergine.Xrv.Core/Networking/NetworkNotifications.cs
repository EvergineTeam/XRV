// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Client;
using Evergine.Networking.Client.Players;
using Evergine.Networking.Rooms;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.Networking
{
    internal class NetworkNotifications : Component
    {
        [BindService]
        private XrvService xrv = null;

        [BindService]
        private MatchmakingClientService client = null;

        [BindService]
        private LocalizationService localization = null;

        private NetworkSystem networkSystem;
        private WindowsSystem windows;
        private LocalNetworkRoom currentRoom;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.networkSystem = this.xrv.Networking;
                this.windows = this.xrv.WindowsSystem;
                this.client.ClientStateChanged += this.Client_ClientStateChanged;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.client.ClientStateChanged -= this.Client_ClientStateChanged;
            this.UnsubscribeFromRoomEvents();
        }

        private void Client_ClientStateChanged(object sender, ClientStates state)
        {
            var session = this.networkSystem.Session;
            switch (state)
            {
                case ClientStates.Joined:
                    this.SubscribeToRoomEvents();

                    if (session.CurrentUserIsHost)
                    {
                        this.NotifyAboutSessionCreated();
                    }
                    else
                    {
                        this.NotifyAboutJoinedToExistingSession();
                    }

                    break;
                case ClientStates.Leaving:
                    this.UnsubscribeFromRoomEvents();
                    break;
                default:
                    break;
            }
        }

        private void SubscribeToRoomEvents()
        {
            this.currentRoom = this.client.CurrentRoom;
            if (this.currentRoom != null)
            {
                this.currentRoom.PlayerJoined += this.CurrentRoom_PlayerJoined;
                this.currentRoom.PlayerLeaving += this.CurrentRoom_PlayerLeaving;
            }
        }

        private void UnsubscribeFromRoomEvents()
        {
            if (this.currentRoom != null)
            {
                this.currentRoom.PlayerJoined -= this.CurrentRoom_PlayerJoined;
                this.currentRoom.PlayerLeaving -= this.CurrentRoom_PlayerLeaving;
            }
        }

        private void CurrentRoom_PlayerJoined(object sender, RemoteNetworkPlayer player)
        {
            if (!this.networkSystem.EnableNotifications)
            {
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Notifications_ParticipantJoined_Title),
                string.Format(
                    this.localization.GetString(() => Resources.Strings.Sessions_Notifications_ParticipantJoined_Text),
                    player.Nickname,
                    this.currentRoom.Name),
                CoreResourcesIDs.Materials.Networking.notification_icon);
        }

        private void CurrentRoom_PlayerLeaving(object sender, RemoteNetworkPlayer player)
        {
            if (!this.networkSystem.EnableNotifications)
            {
                return;
            }

            // If server host is the one who disconnects, it means he has finished the session
            // (actively or not)
            if (player.IsMasterClient)
            {
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Notifications_ParticipantLeft_Title),
                string.Format(
                    this.localization.GetString(() => Resources.Strings.Sessions_Notifications_ParticipantLeft_Text),
                    player.Nickname,
                    this.currentRoom.Name),
                CoreResourcesIDs.Materials.Networking.notification_icon);
        }

        private void NotifyAboutSessionCreated()
        {
            if (!this.networkSystem.EnableNotifications)
            {
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Notifications_CreatedSession_Title),
                string.Format(
                    this.localization.GetString(() => Resources.Strings.Sessions_Notifications_CreatedSession_Text),
                    this.currentRoom.Name,
                    this.client.LocalPlayer.Nickname),
                CoreResourcesIDs.Materials.Networking.notification_icon);
        }

        private void NotifyAboutJoinedToExistingSession()
        {
            if (!this.networkSystem.EnableNotifications)
            {
                return;
            }

            this.windows.ShowNotification(
                this.localization.GetString(() => Resources.Strings.Sessions_Notifications_JoinedSession_Title),
                string.Format(
                    this.localization.GetString(() => Resources.Strings.Sessions_Notifications_JoinedSession_Text),
                    this.currentRoom.Name,
                    this.client.LocalPlayer.Nickname),
                CoreResourcesIDs.Materials.Networking.notification_icon);
        }
    }
}
