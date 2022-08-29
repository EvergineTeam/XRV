// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;

namespace Xrv.Core.Messaging
{
    public abstract class PubSubRegister<TMessage> : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private PubSub pubSub = null;
        private Guid subToken;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.pubSub = this.xrvService.PubSub;
                this.subToken = this.pubSub.Subscribe<TMessage>(this.OnReceived);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.subToken);
        }

        protected abstract void OnReceived(TMessage message);
    }
}
