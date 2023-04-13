// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.Sliders;
using Evergine.Networking.Components;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Networking.Extensions;
using System;

namespace Evergine.Xrv.ImageGallery.Networking
{
    /// <summary>
    /// Network property to synchronize current gallery image. It establishes
    /// image index in associated file access.
    /// </summary>
    public class CurrentImageSynchronization : NetworkIntegerPropertySync<byte>
    {
        [BindService]
        private XrvService xrv = null;

        [BindComponent]
        private Components.ImageGallery gallery = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_slider")]
        private PinchSlider slider = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.gallery.CurrentImageChanged += this.Gallery_CurrentImageChanged;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.gallery.CurrentImageChanged -= this.Gallery_CurrentImageChanged;
        }

        /// <inheritdoc/>
        protected override void OnPropertyAddedOrChanged()
        {
            var sliderValue = 0f;

            if (this.gallery.NumberOfImages > 0)
            {
                sliderValue = (float)this.PropertyValue / (this.gallery.NumberOfImages - 1);
            }

            this.slider.SliderValue = sliderValue;
        }

        /// <inheritdoc/>
        protected override void OnPropertyRemoved()
        {
        }

        private void UpdateValue()
        {
            var session = this.xrv.Networking.Session;
            if (this.IsReady && this.HasInitializedKey() && session.CurrentUserIsPresenter)
            {
                this.PropertyValue = this.gallery.ImageIndex;
            }
        }

        private void Gallery_CurrentImageChanged(object sender, EventArgs e) =>
            this.UpdateValue();
    }
}
