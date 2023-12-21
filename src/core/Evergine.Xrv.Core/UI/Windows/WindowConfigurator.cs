// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System.Linq;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Window configuration component.
    /// </summary>
    public class WindowConfigurator : BaseWindowConfigurator
    {
        private Vector2 logoOffsets = new Vector2(0.03f, 0.025f);
        private Material logoMaterial;
        private Entity logoEntity;
        private bool displayLogo = true;
        private Entity closeButtonEntity;
        private bool showCloseButton = true;
        private Window window;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_logo")]
        private Transform3D logoTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_logo")]
        private MaterialComponent logoMaterialComponent = null;

        /// <summary>
        /// Gets or sets window logo material.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether bottom left icon should be displayed or not.
        /// </summary>
        public bool DisplayLogo
        {
            get => this.displayLogo;

            set
            {
                if (this.displayLogo != value)
                {
                    this.displayLogo = value;
                    this.UpdateDisplayLogo();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether top right close window button should be displayed or not.
        /// </summary>
        public bool ShowCloseButton
        {
            get => this.showCloseButton;

            set
            {
                if (this.showCloseButton != value)
                {
                    this.showCloseButton = value;
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logoEntity = this.Owner.FindChildrenByTag("PART_window_logo", isRecursive: true).First();
                this.closeButtonEntity = this.Owner.FindChildrenByTag("PART_window_close", isRecursive: true).First();
                this.window = this.Owner.FindComponent<Window>(); // TODO: review this reference, maybe refactoring required?
                this.UpdateShowCloseButton();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateLogoMaterial();
        }

        /// <inheritdoc/>
        protected override void UpdateSize()
        {
            base.UpdateSize();

            var halfSize = this.Size * 0.5f;
            var logoTransform = this.logoTransform.LocalPosition;
            logoTransform.X = -halfSize.X + this.logoOffsets.X;
            logoTransform.Y = -halfSize.Y + this.logoOffsets.Y;
            this.logoTransform.LocalPosition = logoTransform;
        }

        /// <inheritdoc/>
        protected override float GetNumberOfActionButtons()
        {
            if (this.window == null)
            {
                return 0;
            }

            return this.window.AllowPin ? 2 : 1;
        }

        private void UpdateLogoMaterial()
        {
            this.logoMaterialComponent.Material = this.logoMaterial;
            this.UpdateDisplayLogo();
        }

        private void UpdateDisplayLogo()
        {
            if (this.IsAttached)
            {
                this.logoEntity.IsEnabled = this.displayLogo && this.logoMaterial != null;
            }
        }

        private void UpdateShowCloseButton()
        {
            if (this.closeButtonEntity != null)
            {
                this.closeButtonEntity.IsEnabled = this.ShowCloseButton;
            }
        }
    }
}
