// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Networking.Components;
using Evergine.Xrv.Core.Networking.Extensions;

namespace Evergine.Xrv.Core.Networking.Properties
{
    /// <summary>
    /// Synchronizes an entity transform.
    /// </summary>
    public class TransformSynchronization : NetworkMatrix4x4PropertySync<byte>
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private Transform3D transform = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformSynchronization"/> class.
        /// </summary>
        public TransformSynchronization()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformSynchronization"/> class.
        /// </summary>
        /// <param name="propertyKey">Property key.</param>
        public TransformSynchronization(byte propertyKey)
            : base()
        {
            this.PropertyKey = propertyKey;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.SubscribeEvents();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();

            if (!Application.Current.IsEditor)
            {
                this.UnsubscribeEvents();
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyReadyToSet()
        {
            base.OnPropertyReadyToSet();
            this.UpdatePropertyValue();
        }

        /// <inheritdoc/>
        protected override void OnPropertyAddedOrChanged()
        {
            this.transform.LocalTransform = this.PropertyValue;
        }

        /// <inheritdoc/>
        protected override void OnPropertyRemoved()
        {
        }

        private void SubscribeEvents()
        {
            if (this.transform != null)
            {
                this.transform.LocalTransformChanged += this.Transform_LocalTransformChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (this.transform != null)
            {
                this.transform.LocalTransformChanged -= this.Transform_LocalTransformChanged;
            }
        }

        private void Transform_LocalTransformChanged(object sender, EventArgs e)
        {
            this.UpdatePropertyValue();
        }

        private void UpdatePropertyValue()
        {
            var session = this.xrvService.Networking.Session;
            if (!session.CurrentUserIsPresenter)
            {
                return;
            }

            /*
             * For Matrix4x4 sync properties we detected that it was not ready on
             * time to send initial transformation value, which provokes incorrect
             * positioning on non-presenter individuals, unless presenter provokes
             * a local transform change for first time. As work around, we create
             * a separated thread to check for property to be ready to send initial position.
             */
            var updateTransform = () => { this.PropertyValue = this.transform.LocalTransform; };
            bool isReady = this.CalculateIsReadyAndInitialized();
            if (isReady)
            {
                updateTransform();
            }
            else
            {
                _ = this.WaitUntilReadyAndExecuteAsync(updateTransform)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error waiting to property ready: {t.Exception}");
                        }
                    });
            }

            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsPresenter)
            {
                this.PropertyValue = this.transform.LocalTransform;
            }
        }

        private bool CalculateIsReadyAndInitialized() => this.IsReady && this.HasInitializedKey();

        private async Task WaitUntilReadyAndExecuteAsync(Action action, CancellationToken cancellation = default)
        {
            const int CheckDelay = 20;
            const int MaxWaitTime = 500;
            bool isReady = this.CalculateIsReadyAndInitialized();
            int currentWaitTime = 0;

            while (!isReady && !cancellation.IsCancellationRequested)
            {
                await Task.Delay(CheckDelay).ConfigureAwait(false);
                currentWaitTime += CheckDelay;

                if (currentWaitTime >= MaxWaitTime)
                {
                    break;
                }
            }

            isReady = this.CalculateIsReadyAndInitialized();
            if (isReady && !cancellation.IsCancellationRequested)
            {
                action.Invoke();
            }
        }
    }
}
