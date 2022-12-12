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

        private ConcurrentQueue<SessionDataGroup> updateQueue;
        private bool isDirty;

        public SessionDataUpdateManager()
        {
            this.updateQueue = new ConcurrentQueue<SessionDataGroup>();
        }

        public void UpdateSession(SessionDataGroup update) => this.updateQueue.Enqueue(update);

        protected override void Update(TimeSpan gameTime)
        {
            if (this.updateQueue.IsEmpty)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(SessionDataUpdateManager)}] Detected {this.updateQueue.Count} enqueued updates");
            while (this.updateQueue.TryDequeue(out var update))
            {
                var sessionData = this.synchronization.Data;
                sessionData.SetGroupData(update);
                this.isDirty = true;
            }

            if (this.isDirty)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(SessionDataUpdateManager)}] Forcing synchronization");
                this.synchronization.ForceSync();
            }

            this.isDirty = false;
        }
    }
}
