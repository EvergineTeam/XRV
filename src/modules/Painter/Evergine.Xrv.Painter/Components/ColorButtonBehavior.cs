// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Painter.Enums;

namespace Evergine.Xrv.Painter.Components
{
    /// <summary>
    /// Component of a pressable button which changes the color of the painter.
    /// </summary>
    public class ColorButtonBehavior : Component
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        [BindComponent]
        private ToggleButton toggleButton = null;

        private PainterManager painterManager = null;

        /// <summary>
        /// Gets or sets the color of the button.
        /// </summary>
        [RenderProperty(Tooltip = "The color that the button sets in the painter")]
        public ColorEnum Color { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.painterManager = this.Owner.FindComponentInParents<PainterManager>();

            return true;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.toggleButton.Toggled += this.ToggleButtonToggled;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.toggleButton.Toggled -= this.ToggleButtonToggled;
        }

        private void ToggleButtonToggled(object sender, System.EventArgs e)
        {
            this.SetSelected(this.toggleButton.IsOn);
        }

        private void SetSelected(bool isOn)
        {
            if (isOn)
            {
                this.painterManager.Color = this.Color;
            }
        }
    }
}
