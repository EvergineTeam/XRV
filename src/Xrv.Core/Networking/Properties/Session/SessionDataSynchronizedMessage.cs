// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.Session
{
    /// <summary>
    /// Session data synchronized message.
    /// </summary>
    public class SessionDataSynchronizedMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionDataSynchronizedMessage"/> class.
        /// </summary>
        /// <param name="data">Session data instance.</param>
        public SessionDataSynchronizedMessage(SessionData data)
        {
            this.Data = data;
        }

        /// <summary>
        /// Gets session data instance.
        /// </summary>
        public SessionData Data { get; private set; }
    }
}
