// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Message to notify about session changes.
    /// </summary>
    public class SessionStatusChangeMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStatusChangeMessage"/> class.
        /// </summary>
        /// <param name="newStatus">New session status.</param>
        public SessionStatusChangeMessage(SessionStatus newStatus)
        {
            this.NewStatus = newStatus;
        }

        /// <summary>
        /// Gets current session status.
        /// </summary>
        public SessionStatus NewStatus { get; private set; }
    }
}
