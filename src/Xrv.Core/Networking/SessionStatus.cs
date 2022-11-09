// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Session status.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// User is disconnected from session.
        /// </summary>
        Disconnected,

        /// <summary>
        /// User is joining to a session.
        /// </summary>
        Joining,

        /// <summary>
        /// User has already joined a session.
        /// </summary>
        Joined,
    }
}
