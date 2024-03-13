// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.ControlRequest;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Controls window UI state depending on current user is presenter of a
    /// networking session or not. It disables window buttons if user has not
    /// session control, and enables them again once he has the control.
    /// </summary>
    public class NetworkingWindowController : SessionControlChangeObserver
    {
        /// <summary>
        /// XRV service.
        /// </summary>
        [BindService]
        protected XrvService xrv = null;

        /// <summary>
        /// Attached window.
        /// </summary>
        [BindComponent]
        protected Window window = null;

        /// <inheritdoc/>
        protected override void OnControlGained()
        {
            base.OnControlGained();
            this.window.EnableManipulation = true;
            this.window.PlaceInFrontOfUserWhenOpened = true;
            this.UpdateButtonsEnableState(true);
        }

        /// <inheritdoc/>
        protected override void OnControlLost()
        {
            base.OnControlLost();
            this.window.EnableManipulation = false;
            this.window.PlaceInFrontOfUserWhenOpened = false;
            this.UpdateButtonsEnableState(false);
        }

        private void UpdateButtonsEnableState(bool enable)
        {
            foreach (var button in this.window.ButtonsOrganizer.ActionBarButtons)
            {
                this.UpdateButtonState(button, enable);
            }

            foreach (var button in this.window.ButtonsOrganizer.MoreActionButtons)
            {
                this.UpdateButtonState(button, enable);
            }
        }

        private void UpdateButtonState(Entity buttonEntity, bool enable)
        {
            var visualController = buttonEntity.FindComponent<VisuallyEnabledController>();
            visualController.IsVisuallyEnabled = enable;
        }
    }
}
