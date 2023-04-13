// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.ControlRequest;

namespace Evergine.Xrv.ImageGallery.Networking
{
    /// <summary>
    /// It controls gallery visual state depending on networking
    /// session control. If user has not the control, it disables
    /// gallery UI elements, to avoid user interaction. If it has the control,
    /// UI elements are enabled again.
    /// </summary>
    public class GallerySessionController : SessionControlChangeObserver
    {
        [BindComponent]
        private Components.ImageGallery gallery = null;

        /// <inheritdoc/>
        protected override void OnControlGained()
        {
            base.OnControlGained();
            this.gallery.IsVisuallyEnabled = true;
        }

        /// <inheritdoc/>
        protected override void OnControlLost()
        {
            base.OnControlLost();
            this.gallery.IsVisuallyEnabled = false;
        }
    }
}
