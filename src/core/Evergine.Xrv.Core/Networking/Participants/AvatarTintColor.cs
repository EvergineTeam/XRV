// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.MRTK.Effects;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Tints a material using avatar reference color.
    /// </summary>
    public class AvatarTintColor : Component
    {
        [BindComponent]
        private MaterialComponent materialComponent = null;
        private HoloGraphic holoGraphic;
        private Color tintColor;

        /// <summary>
        /// Gets or sets material tint color.
        /// </summary>
        [IgnoreEvergine]
        [DontRenderProperty]
        public Color TintColor
        {
            get => this.tintColor;

            set
            {
                if (this.tintColor != value)
                {
                    this.tintColor = value;

                    if (this.IsAttached)
                    {
                        this.OnTintColorUpdate();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.holoGraphic = new HoloGraphic(this.materialComponent.Material);
                this.OnTintColorUpdate();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.holoGraphic?.Dispose();
            this.holoGraphic = null;
        }

        private void OnTintColorUpdate() => this.holoGraphic.Albedo = this.tintColor;
    }
}
