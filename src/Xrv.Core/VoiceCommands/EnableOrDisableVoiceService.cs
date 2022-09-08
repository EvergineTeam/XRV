// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.MRTK.Emulation;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;

namespace Xrv.Core.VoiceCommands
{
    /// <summary>
    /// Enables or disables voice service when a toggle button is toggled.
    /// </summary>
    public class EnableOrDisableVoiceService : Component
    {
        [BindService(isRequired: false)]
        private IVoiceCommandService voiceCommandsService = null;

        [BindComponent]
        private ToggleButton toggleButton = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                Workarounds.ChangeToggleButtonState(this.toggleButton.Owner, true);
            }

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
            this.toggleButton.Toggled -= this.ToggleButton_Toggled;
        }

        private void ToggleButton_Toggled(object sender, EventArgs e)
        {
            if (this.voiceCommandsService is Service service)
            {
                service.IsEnabled = this.toggleButton.IsOn;
            }
        }
    }
}
