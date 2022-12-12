// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Players;

namespace Xrv.Core.Networking.Messaging
{
    internal class NetworkPeerWrapper : INetworkPeer
    {
        public int Id => this.Peer.Id;

        public BaseNetworkPlayer Peer { get; set; }
    }
}
