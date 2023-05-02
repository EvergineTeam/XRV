// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Client;
using Evergine.Networking.Components;
using Evergine.Xrv.Core.Networking.Properties;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /*
     * This is like a standard transform synchronization, but it ensures that
     * only local device is allowed to change its value.
     */
    internal class TransformSynchronizationByClient : TransformSynchronization
    {
        [BindService]
        private MatchmakingClientService client = null;

        [BindComponent(source: BindComponentSource.Parents)]
        private NetworkPlayerProvider networkingProvider = null;

        protected override bool EvaluatePropertyValueUpdate() =>
            this.client.LocalPlayer.Id == this.networkingProvider.PlayerId;
    }
}
