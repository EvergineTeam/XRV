// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;
using System.Collections.Concurrent;

namespace Xrv.Core.Networking.Properties.Session
{
    internal class SessionDataUpdateManager : Behavior
    {
        [BindComponent]
        private SessionDataSynchronization synchronization = null;

        private ConcurrentQueue<IUpdateAction> updateQueue;
        private bool isDirty;

        public SessionDataUpdateManager()
        {
            this.updateQueue = new ConcurrentQueue<IUpdateAction>();
        }

        public bool IsReady { get => this.IsAttached && (this.synchronization?.IsReady ?? false); }

        public void UpdateSession(string propertyName, object propertyValue)
            => this.updateQueue.Enqueue(new UpdateGlobalAction(propertyName, propertyValue));

        public void UpdateSession(SessionDataGroup update) => this.updateQueue.Enqueue(new UpdateGroupAction(update));

        protected override void Update(TimeSpan gameTime)
        {
            if (this.updateQueue.IsEmpty)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(SessionDataUpdateManager)}] Detected {this.updateQueue.Count} enqueued updates");

            var sessionData = this.synchronization.PropertyValue;
            while (this.updateQueue.TryDequeue(out var update))
            {
                update.Update(sessionData);
                this.isDirty = true;
            }

            if (this.isDirty)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(SessionDataUpdateManager)}] Forcing synchronization");
                this.synchronization.PropertyValue = sessionData;
                this.synchronization.ForceSync();
            }

            this.isDirty = false;
        }
    }
}
