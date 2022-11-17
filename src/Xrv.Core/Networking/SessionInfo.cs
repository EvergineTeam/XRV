// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Xrv.Core.Messaging;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Active session data.
    /// </summary>
    public class SessionInfo
    {
        private readonly PubSub pubSub;
        private SessionStatus status;

        internal SessionInfo(PubSub pubSub)
        {
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
        public bool CurrentUserIsHost { get; internal set; }

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
    }
}
