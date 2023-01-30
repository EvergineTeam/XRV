// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Networking.Components;
using System;
using Evergine.Xrv.Core.Networking.Extensions;

namespace Evergine.Xrv.Core.Modules
{
    /// <summary>
    /// Synchronizes module activation status simulating the press of
    /// associated hand menu button. It works for toggle buttons only.
    /// </summary>
    public class ModuleActivationSync : NetworkBooleanPropertySync<byte>
    {
        [BindService]
        private XrvService xrvService = null;

        private ActivateModuleOnButtonPress activationOnPress = null;
        private ToggleButton handMenuButton = null;
        private Guid subscription;

        /// <summary>
        /// Gets or sets target module.
        /// </summary>
        [IgnoreEvergine]
        public Module Module { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                var moduleButton = this.xrvService.HandMenu.GetModuleButtonEntity(this.Module);
                this.activationOnPress = moduleButton?.FindComponent<ActivateModuleOnButtonPress>();
                this.handMenuButton = moduleButton?.FindComponentInChildren<ToggleButton>();
                this.subscription = this.xrvService.Services.Messaging.Subscribe<ActivateModuleMessage>(this.OnModuleActivationChange);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.xrvService.Services.Messaging.Unsubscribe(this.subscription);
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
            this.activationOnPress?.SetModuleActivationState(this.PropertyValue);
        }

        /// <inheritdoc/>
        protected override void OnPropertyRemoved()
        {
        }

        private void OnModuleActivationChange(ActivateModuleMessage message) => this.UpdatePropertyValue();

        private void UpdatePropertyValue()
        {
            if (Application.Current.IsEditor || this.PropertyKeyByte == default)
            {
                return;
            }

            // TODO we should consider user with session control is
            var session = this.xrvService.Networking.Session;
            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsPresenter)
            {
                this.PropertyValue = this.handMenuButton.IsOn;
            }
        }
    }
}
