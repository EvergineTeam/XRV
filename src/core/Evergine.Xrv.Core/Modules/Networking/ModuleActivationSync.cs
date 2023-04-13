// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Networking.Components;
using System;
using Evergine.Xrv.Core.Networking.Extensions;

namespace Evergine.Xrv.Core.Modules.Networking
{
    /// <summary>
    /// Synchronizes module activation status when a module activation message is emitted.
    /// </summary>
    public class ModuleActivationSync : NetworkBooleanPropertySync<byte>
    {
        [BindService]
        private XrvService xrvService = null;

        private ActivateModuleOnButtonPress activationOnPress = null;
        private Guid subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleActivationSync"/> class.
        /// </summary>
        /// <param name="module">Target module.</param>
        public ModuleActivationSync(Module module)
        {
            this.Module = module;
        }

        /// <summary>
        /// Gets target module.
        /// </summary>
        [IgnoreEvergine]
        public Module Module { get; private set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                var moduleButton = this.xrvService.HandMenu.GetModuleButtonEntity(this.Module);
                this.activationOnPress = moduleButton?.FindComponent<ActivateModuleOnButtonPress>();
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
            this.UpdatePropertyValue(false);
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

        private void OnModuleActivationChange(ActivateModuleMessage message)
        {
            if (message.Module == this.Module)
            {
                this.UpdatePropertyValue(message.IsOn);
            }
        }

        private void UpdatePropertyValue(bool isModuleOn)
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            var session = this.xrvService.Networking.Session;
            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsPresenter)
            {
                this.PropertyValue = isModuleOn;
            }
        }
    }
}
