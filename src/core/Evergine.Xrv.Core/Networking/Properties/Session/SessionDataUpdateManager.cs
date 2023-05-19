// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Evergine.Xrv.Core.Networking.Properties.Session
{
    internal class SessionDataUpdateManager : Behavior
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private SessionDataSynchronization synchronization = null;

        private ConcurrentQueue<IUpdateAction> updateQueue;
        private bool isDirty;
        private ILogger logger;

        public SessionDataUpdateManager()
        {
            this.updateQueue = new ConcurrentQueue<IUpdateAction>();
        }

        public bool IsReady { get => this.IsAttached && (this.synchronization?.IsReady ?? false); }

        public void UpdateSession(string propertyName, object propertyValue)
            => this.updateQueue.Enqueue(new UpdateGlobalAction(propertyName, propertyValue));

        public void UpdateSession(SessionDataGroup update) => this.updateQueue.Enqueue(new UpdateGroupAction(update));

        public void ClearSession()
        {
            this.updateQueue.ClearImpl();
            this.synchronization.SetData(null);
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        protected override void Update(TimeSpan gameTime)
        {
            var sessionData = this.synchronization.CurrentValue;
            if (this.updateQueue.IsEmpty || sessionData == null)
            {
                return;
            }

            this.logger?.LogDebug($"Session data update detected. There are {this.updateQueue.Count} enqueued updates");

            while (this.updateQueue.TryDequeue(out var update))
            {
                update.Update(sessionData);
                this.isDirty = true;
            }

            if (this.isDirty)
            {
                this.logger?.LogDebug("Forcing session data update synchronization");
                this.logger?.LogDebug(sessionData.ToString());
                this.synchronization.SetData(sessionData);
            }

            this.isDirty = false;
        }
    }
}
