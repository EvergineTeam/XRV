using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xrv.Core.Messaging
{
    public class PubSub
    {
        private Dictionary<Type, IList> subscriptions;

        public PubSub()
        {
            subscriptions = new Dictionary<Type, IList>();
        }

        public Guid Subscribe<TMessage>(Action<TMessage> action)
        {
            var type = typeof(TMessage);

            if (!subscriptions.ContainsKey(type))
            {
                subscriptions.Add(type, new List<Subscription<TMessage>>());
            }

            var typeSubs = subscriptions[type];
            var newSub = new Subscription<TMessage>
            {
                Action = action,
            };
            typeSubs.Add(newSub);

            return newSub.Token;
        }

        public void Unsubscribe(Guid token)
        {
            foreach (var typeSubs in subscriptions.Values)
            {
                foreach (var item in typeSubs.OfType<Subscription>())
                {
                    if (item.Token == token)
                    {
                        typeSubs.Remove(item);
                        return;
                    }
                }
            }
        }

        public void Publish<TMessage>(TMessage message)
        {
            var type = typeof(TMessage);
            if (subscriptions.ContainsKey(type))
            {
                var subscriptions = this.subscriptions[type].OfType<Subscription<TMessage>>();
                foreach (var subscription in subscriptions)
                {
                    subscription.Action(message);
                }
            }
        }

        private abstract class Subscription
        {
            public Subscription()
            {
                Token = Guid.NewGuid();
            }

            public Guid Token { get; private set; }
        }

        private class Subscription<T> : Subscription
        {

            public Action<T> Action { get; set; }
        }
    }
}
