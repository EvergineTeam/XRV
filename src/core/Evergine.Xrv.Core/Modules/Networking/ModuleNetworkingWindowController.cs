// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Xrv.Core.UI.Windows;
using System;

namespace Evergine.Xrv.Core.Modules.Networking
{
    /// <summary>
    /// Listens to window closing to mark send a deactivation signal
    /// for associated module, and make this to be synchronized in a networking
    /// session.
    /// </summary>
    public class ModuleNetworkingWindowController : NetworkingWindowController
    {
        private ActivateModuleOnButtonPress activationOnPress = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleNetworkingWindowController"/> class.
        /// </summary>
        /// <param name="module">Associated module.</param>
        public ModuleNetworkingWindowController(Module module)
        {
            this.Module = module;
        }

        /// <summary>
        /// Gets associated module.
        /// </summary>
        [IgnoreEvergine]
        public Module Module { get; private set; }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            var moduleButton = this.xrv.HandMenu.GetModuleButtonEntity(this.Module);
            this.activationOnPress = moduleButton?.FindComponent<ActivateModuleOnButtonPress>();

            this.window.Closing += this.Window_Closing;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.window.Closing -= this.Window_Closing;
        }

        private void Window_Closing(object sender, EventArgs e) =>
            this.activationOnPress?.SetModuleActivationState(false);
    }
}
