// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Xrv.Core.Services.Messaging
{
    /// <summary>
    /// Simple publisher-subscriber implementation. It allows messaging
    /// using classes, so delivered information could be as complex as required.
    /// </summary>
    public class PubSub
    {
        private Dictionary<Type, IList> subscriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubSub"/> class.
        /// </summary>
        public PubSub()
        {
            this.subscriptions = new Dictionary<Type, IList>();
        }

        /// <summary>
        /// Subscribes to a given message type.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="action">Action to be executed when a message of this type is notified.</param>
        /// <returns>Subscription token.</returns>
        public Guid Subscribe<TMessage>(Action<TMessage> action)
        {
            var type = typeof(TMessage);

            if (!this.subscriptions.ContainsKey(type))
            {
                this.subscriptions.Add(type, new List<Subscription<TMessage>>());
            }

            var typeSubs = this.subscriptions[type];
            var newSub = new Subscription<TMessage>
            {
                Action = action,
            };
            typeSubs.Add(newSub);

            return newSub.Token;
        }

        /// <summary>
        /// Unsubscribes from messaging bus.
        /// </summary>
        /// <param name="token">Subscription token.</param>
        public void Unsubscribe(Guid token)
        {
            foreach (var typeSubs in this.subscriptions.Values)
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

        /// <summary>
        /// Notifies about a message.
        /// </summary>
        /// <typeparam name="TMessage">Message type.</typeparam>
        /// <param name="message">Message instance.</param>
        public void Publish<TMessage>(TMessage message)
        {
            var type = typeof(TMessage);
            if (this.subscriptions.ContainsKey(type))
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
                this.Token = Guid.NewGuid();
            }

            public Guid Token { get; private set; }
        }

        private class Subscription<T> : Subscription
        {
            public Action<T> Action { get; set; }
        }
    }
}
