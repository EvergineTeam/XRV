// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking;
using System;
using System.Collections.Concurrent;
using Xrv.Core.Messaging;

namespace Xrv.Core.Networking.Properties.Session
{
    /// <summary>
    /// Synchronizes session group information for a given group.
    /// </summary>
    /// <typeparam name="TData">Group data type.</typeparam>
    public abstract class DataGroupSynchronization<TData> : Behavior
        where TData : class, INetworkSerializable
    {
        private readonly ConcurrentQueue<Action<TData>> updateQueue;

        [BindService]
        private XrvService xrvService = null;

        private PubSub pubSub;
        private Guid sessionStatusToken;
        private Guid sessionSyncToken;
        private TData data;
        private bool isDirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGroupSynchronization{TData}"/> class.
        /// </summary>
        public DataGroupSynchronization()
        {
            this.updateQueue = new ConcurrentQueue<Action<TData>>();
        }

        /// <summary>
        /// Gets data synchronization group name.
        /// </summary>
        public abstract string GroupName { get; }

        /// <summary>
        /// Creates initial instance for data group.
        /// </summary>
        /// <returns>Data instance.</returns>
        public abstract TData CreateInitialInstance();

        /// <summary>
        /// Updates group data value.
        /// </summary>
        /// <param name="update">Update action.</param>
        public void UpdateData(Action<TData> update) => this.updateQueue.Enqueue(update);

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.pubSub = this.xrvService.PubSub;
                this.sessionStatusToken = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
                this.sessionSyncToken = this.pubSub.Subscribe<SessionDataSynchronizedMessage>(this.OnSessionDataSynchronized);
                this.IsEnabled = false;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.sessionStatusToken);
            this.pubSub.Unsubscribe(this.sessionSyncToken);
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            while (this.updateQueue.TryDequeue(out var update))
            {
                update.Invoke(this.data);
                this.isDirty = true;
            }

            if (this.isDirty)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(DataGroupSynchronization<TData>)}] Detected changes for group {this.GroupName}: requesting update");
                this.RequestDataUpdate();
            }

            this.isDirty = false;
        }

        /// <summary>
        /// Invoked when group data has been synchronized.
        /// </summary>
        /// <param name="data">Group data instance.</param>
        protected abstract void OnDataSynchronized(TData data);

        /// <summary>
        /// Invoked when session disconnection has been detected.
        /// </summary>
        protected abstract void OnSessionDisconnection();

        private void OnSessionDataSynchronized(SessionDataSynchronizedMessage message)
        {
            System.Diagnostics.Debug.WriteLine($"[{nameof(DataGroupSynchronization<TData>)}] Session data synchronized: updating data for {this.GroupName}");

            var sessionData = message.Data;
            if (sessionData.TryGetGroupData<TData>(this.GroupName, out TData data))
            {
                this.data = data;
                this.OnDataSynchronized(data);
            }
        }

        private void OnSessionStatusChange(SessionStatusChangeMessage message)
        {
            this.IsEnabled = message.NewStatus == SessionStatus.Joined;

            var networking = this.xrvService.Networking;
            if (this.IsEnabled && networking.Session.CurrentUserIsHost)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(DataGroupSynchronization<TData>)}] Initializing data group {this.GroupName}");
                this.data = this.CreateInitialInstance();
                this.RequestDataUpdate();
            }
            else if (!this.IsEnabled)
            {
                this.OnSessionDisconnection();
            }
        }

        private void RequestDataUpdate()
        {
            var networking = this.xrvService.Networking;
            var protocol = new UpdateSessionDataProtocol(networking, networking.SessionDataUpdateManager);
            _ = protocol.UpdateDataAsync(this.GroupName, this.data)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error synchronizing group {this.GroupName} data: {t.Exception}");
                    }
                });
        }
    }
}
