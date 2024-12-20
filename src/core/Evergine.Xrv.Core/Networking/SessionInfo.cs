﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Client;
using Evergine.Xrv.Core.Networking.Properties.Session;
using Evergine.Xrv.Core.Services.Messaging;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Active session data.
    /// </summary>
    public class SessionInfo
    {
        private readonly MatchmakingClientService client;
        private readonly PubSub pubSub;
        private SessionStatus status;
        private SessionDataSynchronization sessionDataSync;

        internal SessionInfo(MatchmakingClientService client, PubSub pubSub)
        {
            this.client = client;
            this.pubSub = pubSub;
        }

        /// <summary>
        /// Gets session host information.
        /// </summary>
        public SessionHostInfo Host { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether device user is the one who
        /// created the session.
        /// </summary>
        public virtual bool CurrentUserIsHost { get => this.client.LocalPlayer.IsMasterClient; }

        /// <summary>
        /// Gets a value indicating whether current user is session presenter.
        /// </summary>
        public bool CurrentUserIsPresenter
        {
            get
            {
                if (this.Data != null)
                {
                    return this.client.LocalPlayer.Id == this.Data.PresenterId;
                }
                else
                {
                    return this.CurrentUserIsHost;
                }
            }
        }

        /// <summary>
        /// Gets session data object. Be careful, direct modifications
        /// over this object will not be propagated through other clients. Use
        /// appropiate mechanism for that.
        /// </summary>
        public SessionData Data { get => this.sessionDataSync.CurrentValue; }

        /// <summary>
        /// Gets current session status.
        /// </summary>
        public SessionStatus Status
        {
            get => this.status;
            internal set
            {
                if (this.status != value)
                {
                    this.status = value;
                    this.pubSub.Publish(new SessionStatusChangeMessage(this.status));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether session has been actively closed by the client.
        /// This is, user has tapped on disconnection button.
        /// </summary>
        public bool ActivelyClosedByClient { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether session has been actively closed by the host.
        /// This is, host has tapped on session end button.
        /// </summary>
        public bool ActivelyClosedByHost { get; internal set; }

        internal void SetData(SessionDataSynchronization sessionDataSync) => this.sessionDataSync = sessionDataSync;
    }
}
