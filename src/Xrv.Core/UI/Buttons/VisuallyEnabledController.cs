// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Physics3D;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;

namespace Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Makes a button to be visually enabled or disabled. When marked as disabled,
    /// also disables some internal components to avoid button from being pressable.
    /// </summary>
    public class VisuallyEnabledController : Component
    {
        private bool isVisuallyEnabled = true;

        [BindComponent(source: BindComponentSource.Children, isRecursive: true)]
        private BoxCollider3D boxCollider = null;

        [BindComponent(source: BindComponentSource.Children, isRecursive: true)]
        private PressableButton pressableButton = null;

        [BindComponent(source: BindComponentSource.Children, isRecursive: true, tag: "PART_Icon")]
        private MaterialComponent iconMaterialComponent = null;

        private HoloGraphic iconHoloGraphic;

        /// <summary>
        /// Gets or sets a value indicating whether button is visually enabled.
        /// </summary>
        public bool IsVisuallyEnabled
        {
            get => this.isVisuallyEnabled;

            set
            {
                if (this.isVisuallyEnabled != value)
                {
                    this.isVisuallyEnabled = value;
                    this.UpdateIsVisuallyEnabled();
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.InstantiateHoloGraphic();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.InstantiateHoloGraphic();
            this.UpdateIsVisuallyEnabled();
        }

        private void InstantiateHoloGraphic()
        {
            if (this.iconHoloGraphic == null && this.iconMaterialComponent.Material != null)
            {
                this.iconHoloGraphic = new HoloGraphic(this.iconMaterialComponent.Material);
            }
        }

        private void UpdateIsVisuallyEnabled()
        {
            if (!this.IsAttached)
            {
                return;
            }

            this.boxCollider.IsEnabled = this.isVisuallyEnabled;
            this.pressableButton.IsEnabled = this.isVisuallyEnabled;
            this.iconHoloGraphic.Parameters_Alpha = this.isVisuallyEnabled ? 1f : 0.5f;
        }
    }
}
