// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Client;
using Evergine.Xrv.Core.Networking.Properties.Session;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Evergine.Xrv.Core.Networking.ControlRequest
{
    internal class SessionPresenterStillAlive : Behavior
    {
        [BindService]
        private XrvService xrv = null;

        [BindService]
        private MatchmakingClientService client = null;

        [BindComponent]
        private SessionDataUpdateManager updateManager = null;

        private NetworkSystem networking = null;
        private ILogger logger = null;
        private TimeSpan elapsedTime;

        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(5);

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.networking = this.xrv.Networking;
                this.logger = this.xrv.Services.Logging;
            }

            return attached;
        }

        protected override void Update(TimeSpan gameTime)
        {
            this.elapsedTime += gameTime;
            if (this.elapsedTime <= this.CheckInterval)
            {
                return;
            }

            this.elapsedTime = TimeSpan.Zero;

            var room = this.client.CurrentRoom;
            if (room == null)
            {
                return;
            }

            var data = this.networking.Session.Data;
            var currentPresenterId = data.PresenterId;
            bool clientIsConnected = room.RemotePlayers.Any(player => player.Id == currentPresenterId);
            if (clientIsConnected)
            {
                return;
            }

            var thisClientId = this.client.LocalPlayer.Id;
            if (thisClientId == currentPresenterId)
            {
                return;
            }

            this.updateManager.UpdateSession(nameof(SessionData.PresenterId), thisClientId);
            this.logger?.LogDebug($"Missing user {currentPresenterId} as presenter; setting session host as new presenter");
        }
    }
}
