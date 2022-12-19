// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.Session
{
    /// <summary>
    /// Session data upate action.
    /// </summary>
    internal interface IUpdateAction
    {
        /// <summary>
        /// Updates session data.
        /// </summary>
        /// <param name="data">Session data.</param>
        void Update(SessionData data);
    }
}
