// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.ControlRequest;
using Evergine.Xrv.Core.UI.Buttons;
using System.Collections.Generic;
using System.Linq;

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

        private Entity closeButton;
        private Entity followButton;
        private List<Entity> allButtons;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.closeButton = this.Owner.FindChildrenByTag("PART_window_close", true).First();
                this.followButton = this.Owner.FindChildrenByTag("PART_window_follow", true).First();
                this.allButtons = new List<Entity> { this.closeButton, this.followButton };
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnControlGained()
        {
            base.OnControlGained();
            this.window.EnableManipulation = true;
            this.window.PlaceInFrontOfUserWhenOpened = true;
            this.UpdateButtonEnableState(true);
        }

        /// <inheritdoc/>
        protected override void OnControlLost()
        {
            base.OnControlLost();
            this.window.EnableManipulation = false;
            this.window.PlaceInFrontOfUserWhenOpened = false;
            this.UpdateButtonEnableState(false);
        }

        private void UpdateButtonEnableState(bool enable)
        {
            this.allButtons.ForEach(button =>
            {
                var visualController = button.FindComponent<VisuallyEnabledController>();
                visualController.IsVisuallyEnabled = enable;
            });
        }
    }
}
