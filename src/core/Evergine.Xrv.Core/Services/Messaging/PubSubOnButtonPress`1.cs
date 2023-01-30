// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;

namespace Evergine.Xrv.Core.Services.Messaging
{
    /// <summary>
    /// Publishes a message once button is pressed.
    /// </summary>
    /// <typeparam name="TMessage">Message type.</typeparam>
    public abstract class PubSubOnButtonPress<TMessage> : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Children, isExactType: false)]
        private PressableButton button = null;

        [BindComponent(source: BindComponentSource.Children, isExactType: false, isRequired: false)]
        private ToggleStateManager toggleStateManager = null;

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.toggleStateManager != null)
            {
                this.toggleStateManager.StateChanged += this.StateChangedEvent;
            }
            else
            {
                this.button.ButtonReleased += this.StateChangedEvent;
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.toggleStateManager != null)
            {
                this.toggleStateManager.StateChanged -= this.StateChangedEvent;
            }
            else
            {
                this.button.ButtonReleased -= this.StateChangedEvent;
            }
        }

        /// <summary>
        /// Retrieves message instance, depending on button state.
        /// </summary>
        /// <param name="isOn">For toggle buttons, it indicates toggle state.
        /// Always true for standard buttons.</param>
        /// <returns>Message instance.</returns>
        protected abstract TMessage GetPublishData(bool isOn);

        /// <summary>
        /// Notifies toggle button state change.
        /// </summary>
        /// <param name="isOn">Toggle button state.</param>
        protected void NotifyChange(bool isOn) =>
            this.xrvService.Services.Messaging.Publish(this.GetPublishData(isOn));

        private void StateChangedEvent(object sender, EventArgs e)
        {
            bool isOn = this.toggleStateManager != null ? this.toggleStateManager.CurrentState.Value == ToggleState.On : true;
            this.NotifyChange(isOn);
        }
    }
}
