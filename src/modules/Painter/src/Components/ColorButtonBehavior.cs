// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using Xrv.Core.UI.Buttons;
using Xrv.Painter.Enums;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Component of a pressable button which changes the color of the painter.
    /// </summary>
    public class ColorButtonBehavior : Behavior
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        [BindComponent]
        private ToggleButton toggleButton = null;

        [BindComponent(isRequired: false)]
        private XrvPressableButtonLookAndFeel lookAndFeel = null;

        [BindEntity(isRequired: false, source: BindEntitySource.ChildrenSkipOwner, tag: "Hover", isRecursive: false)]
        private readonly Entity hoverEntity;

        /// <summary>
        /// Gets or sets the color of the button.
        /// </summary>
        [RenderProperty(Tooltip = "The color that the button sets in the painter")]
        public ColorEnum Color { get; set; }

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

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.lookAndFeel != null)
            {
                if (this.lookAndFeel.IsDetected)
                {
                    if (this.hoverEntity != null && !this.hoverEntity.IsEnabled)
                    {
                        this.hoverEntity.IsEnabled = true;
                    }
                }
                else
                {
                    if (this.hoverEntity != null && this.hoverEntity.IsEnabled)
                    {
                        this.hoverEntity.IsEnabled = false;
                    }
                }
            }
        }

        private void ToggleButtonToggled(object sender, System.EventArgs e)
        {
            // TODO: Set color in painter
            ////this.SetSelected(this.toggleButton.IsOn);
        }

        ////private void SetSelected(bool isOn)
        ////{
        ////
        ////}
    }
}
