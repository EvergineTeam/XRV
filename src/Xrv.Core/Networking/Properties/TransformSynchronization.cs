// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Networking.Components;
using System;
using Xrv.Core.Networking.Extensions;

namespace Xrv.Core.Networking.Properties
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

        /// <summary>
        /// Gets or sets property key.
        /// </summary>
        [DontRenderProperty]
        [IgnoreEvergine]
        public new byte PropertyKey
        {
            get => base.PropertyKey;
            set => base.PropertyKey = value;
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
            // TODO we should consider user with session control is
            var session = this.xrvService.Networking.Session;
            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsHost)
            {
                this.PropertyValue = this.transform.LocalTransform;
            }
        }
    }
}
