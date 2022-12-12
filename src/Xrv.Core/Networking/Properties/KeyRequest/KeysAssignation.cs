// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Networking.Components;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xrv.Core.Messaging;

namespace Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Handles networking keys assignation. Invokes <see cref="KeyRequestProtocol"/> to
    /// ask server for free keys.
    /// </summary>
    [AllowMultipleInstances]
    public abstract class KeysAssignation : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private bool inProgress;
        private KeyRequestProtocol keyRequestProtocol;
        private CancellationTokenSource protocolStartCts;
        private PubSub pubSub;
        private Guid subscription;

        /// <summary>
        /// Gets or sets number of required keys.
        /// </summary>
        public byte NumberOfRequiredKeys { get; set; } = 1;

        /// <summary>
        /// Gets or sets property filter.
        /// </summary>
        public NetworkPropertyProviderFilter Filter { get; set; } = NetworkPropertyProviderFilter.Room;

        /// <summary>
        /// Gets assigned keys, if protocol is completed succesfully.
        /// </summary>
        public byte[] AssignedKeys { get; private set; }

        /// <summary>
        /// Gets a value indicating whether keys have been assigned.
        /// </summary>
        public bool HasAssignedKeys { get => this.AssignedKeys?.Length > 0; }

        /// <summary>
        /// Resets keys assignation.
        /// </summary>
        public void Reset()
        {
            this.AssignedKeys = null;
            this.OnKeysAssigned(null);
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.pubSub = this.xrvService.PubSub;
                this.subscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.subscription);
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (!Application.Current.IsEditor)
            {
                this.CheckAndStartKeyRequest();
            }
        }

        /// <summary>
        /// Invoked when keys are assigned.
        /// </summary>
        /// <param name="keys">Assigned keys.</param>
        protected abstract void OnKeysAssigned(byte[] keys);

        private void OnSessionStatusChange(SessionStatusChangeMessage message)
        {
            if (message.NewStatus == SessionStatus.Joined)
            {
                this.CheckAndStartKeyRequest();
            }
        }

        private void CheckAndStartKeyRequest()
        {
            var session = this.xrvService.Networking;
            if (!this.HasAssignedKeys
                &&
                session.Session.Status == SessionStatus.Joined
                &&
                session.Session.CurrentUserIsHost)
            {
                this.protocolStartCts?.Cancel();
                this.protocolStartCts = new CancellationTokenSource();
                _ = this.StartKeysRequestAsync(this.protocolStartCts.Token);
            }
        }

        private async Task StartKeysRequestAsync(CancellationToken cancellationToken)
        {
            if (this.inProgress || this.HasAssignedKeys)
            {
                return;
            }

            this.inProgress = true;

            try
            {
                this.keyRequestProtocol = new KeyRequestProtocol(this.xrvService.Networking);
                this.AssignedKeys = await this.keyRequestProtocol.RequestSetOfKeysAsync(
                    this.NumberOfRequiredKeys,
                    this.Filter,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Key assignation failed: {exception}");
            }

            if (this.HasAssignedKeys && !cancellationToken.IsCancellationRequested)
            {
                this.OnKeysAssigned(this.AssignedKeys);
            }

            this.inProgress = false;
        }
    }
}
