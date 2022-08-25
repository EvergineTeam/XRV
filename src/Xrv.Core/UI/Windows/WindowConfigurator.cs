// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;

namespace Xrv.Core.UI.Windows
{
    public class WindowConfigurator : BaseWindowConfigurator
    {
        private Vector2 logoOffsets = new Vector2(0.03f, 0.025f);
        private Material logoMaterial;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_logo")]
        protected Transform3D logoTransform;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_logo")]
        protected MaterialComponent logoMaterialComponent;

        public Material LogoMaterial
        {
            get => this.logoMaterial;
            set
            {
                this.logoMaterial = value;
                if (this.IsAttached)
                {
                    this.UpdateLogoMaterial();
                }
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateLogoMaterial();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();

            var halfSize = this.Size * 0.5f;
            var logoTransform = this.logoTransform.LocalPosition;
            logoTransform.X = -halfSize.X + this.logoOffsets.X;
            logoTransform.Y = -halfSize.Y + this.logoOffsets.Y;
            this.logoTransform.LocalPosition = logoTransform;
        }

        private void UpdateLogoMaterial()
        {
            this.logoMaterialComponent.Owner.IsEnabled = this.logoMaterial != null;
            this.logoMaterialComponent.Material = this.logoMaterial;
        }
    }
}
