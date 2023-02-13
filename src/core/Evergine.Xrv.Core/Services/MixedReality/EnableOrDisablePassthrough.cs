// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.UI.Buttons;
using System;

namespace Evergine.Xrv.Core.Services.MixedReality
{
    /// <summary>
    /// Enables or disable passthrough using a <see cref="ToggleButton"/>.
    /// </summary>
    public class EnableOrDisablePassthrough : Component
    {
        [BindService]
        private XrvService xrv = null;

        [BindComponent]
        private ToggleButton toggleButton = null;

        [BindComponent]
        private VisuallyEnabledController visuallyEnabledController = null;

        private PasstroughService passthroughService;
        private bool ignoreToggleStateChanges;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.passthroughService = this.xrv.Services.Passthrough;
                this.UpdateButtonStatus();
            }

            this.ignoreToggleStateChanges = true;

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.toggleButton.Toggled += this.ToggleButton_Toggled;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.ignoreToggleStateChanges = true;
            this.toggleButton.Toggled -= this.ToggleButton_Toggled;
        }

        private void ToggleButton_Toggled(object sender, EventArgs args)
        {
            // ToggleStateManager forces a default state that may provoke an
            // accidental change of passthroug state. We just ignore this first
            // toggle event invoke, and listen to laters (made by user, BTW).
            if (this.ignoreToggleStateChanges)
            {
                this.ignoreToggleStateChanges = false;
            }
            else if (this.toggleButton.IsOn)
            {
                this.passthroughService?.Enable();
            }
            else
            {
                this.passthroughService?.Disable();
            }

            this.UpdateButtonStatus();
        }

        private void UpdateButtonStatus()
        {
            bool isSupported = this.passthroughService?.IsSupported == true;
            this.visuallyEnabledController.IsVisuallyEnabled = isSupported;
            if (!isSupported)
            {
                Workarounds.ChangeToggleButtonState(this.toggleButton.Owner, false);
                return;
            }

            bool isRunning = this.passthroughService?.PassthroughComponent.IsRunning ?? false;
            Workarounds.ChangeToggleButtonState(this.toggleButton.Owner, isRunning);
        }
    }
}
