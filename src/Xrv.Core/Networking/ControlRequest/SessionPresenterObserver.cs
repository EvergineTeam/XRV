// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Server;
using Evergine.Networking.Server.Players;
using System;
using Xrv.Core.Messaging;
using Xrv.Core.Networking.Properties.Session;

namespace Xrv.Core.Networking.ControlRequest
{
    internal class SessionPresenterObserver : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private MatchmakingServerService server = null;

        [BindComponent]
        private SessionDataUpdateManager updateManager = null;

        private NetworkSystem networking = null;
        private PubSub pubSub = null;
        private Guid subscription;
        private int? lastPresenterId;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.networking = this.xrvService.Networking;
                this.pubSub = this.xrvService.PubSub;
                this.subscription = this.pubSub.Subscribe<SessionDataSynchronizedMessage>(this.OnSessionDataChanged);
                this.server.PlayerLeaving += this.Server_PlayerLeaving;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            this.lastPresenterId = null;
            this.pubSub.Unsubscribe(this.subscription);
            this.server.PlayerLeaving -= this.Server_PlayerLeaving;
        }

        private void OnSessionDataChanged(SessionDataSynchronizedMessage message)
        {
            if (message.Data != null && this.lastPresenterId != message.Data.PresenterId)
            {
                this.lastPresenterId = message.Data.PresenterId;

                bool currentIsPresenter = this.networking.Client.ClientId == this.lastPresenterId;
                this.pubSub.Publish(new SessionPresenterUpdatedMessage(currentIsPresenter, this.lastPresenterId.Value));
            }
        }

        private void Server_PlayerLeaving(object sender, ServerPlayer player)
        {
            var data = this.networking.Session.Data;
            if (data != null && data.PresenterId == player.Id)
            {
                var serverClientId = this.networking.Client.ClientId;
                this.updateManager.UpdateSession(nameof(SessionData.PresenterId), serverClientId);
            }
        }
    }
}
