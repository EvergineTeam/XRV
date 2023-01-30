// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;

namespace Evergine.Xrv.Core.Services.Messaging
{
    /// <summary>
    /// Registers to publisher-subscriber specific message type.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
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
                this.pubSub = this.xrvService.Services.Messaging;
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

        /// <summary>
        /// Invoked when a message of registered type is received.
        /// </summary>
        /// <param name="message">Message instance.</param>
        protected abstract void OnReceived(TMessage message);
    }
}
