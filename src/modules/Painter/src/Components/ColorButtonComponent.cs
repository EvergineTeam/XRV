// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Diagnostics;
using Xrv.Painter.Enums;
using Xrv.Painter.Helpers;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Component of a pressable button which changes the color of the painter.
    /// </summary>
    public class ColorButtonComponent : Component
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "FrontPlate")]
        private MaterialComponent frontPlateMaterialComponent = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Plate")]
        private MaterialComponent backPlateMaterialComponent = null;

        [BindComponent]
        private ToggleButton toggleButton = null;

        //[BindComponent(source: BindComponentSource.Scene)]
        private PainterManager painterManager = null;

        /// <summary>
        /// Gets or sets the color of the button.
        /// </summary>
        [RenderProperty(Tooltip = "The color of the button and the color that it enables")]
        public ColorEnum Color { get; set; }

        /// <summary>
        /// Gets or sets the color of the backplate of the selected button.
        /// </summary>
        [RenderProperty(Tooltip = "The color of the backplate of the selected button")]
        public Material BackplateSelectedButtonMaterial { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            if (this.frontPlateMaterialComponent != null)
            {
                this.frontPlateMaterialComponent.Material = this.assetsService.Load<Material>(ColorHelper.GetMaterialFromColor(this.Color));
            }

            if (this.backPlateMaterialComponent != null)
            {
                this.backPlateMaterialComponent.Material = this.BackplateSelectedButtonMaterial;
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
            ////if (isOn)
            ////{
            ////    this.backPlateMaterialComponent.Material = this.BackplateSelectedButtonMaterial;
            ////} else
            ////{
            ////    this.backPlateMaterialComponent.Material = this.ColorButtonMaterial;
            ////}
            if (isOn)
            {
                Debug.WriteLine(this.Color);
            }
            else
            {
                Debug.WriteLine("OFF");
            }

        }
    }
}
