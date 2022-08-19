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

        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.subToken);
        }

        protected abstract void OnReceived(TMessage message);
    }
}
