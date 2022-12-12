// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Networking peer.
    /// </summary>
    public interface INetworkPeer
    {
        /// <summary>
        /// Gets client identifier.
        /// </summary>
        int Id { get; }
    }
}
