// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;

namespace Evergine.Xrv.Core.Localization
{
    /// <summary>
    /// Controls localization for <see cref="Text3DMesh"/>.
    /// </summary>
    public class Text3dLocalization : BaseLocalization
    {
        [BindComponent]
        private Text3DMesh text = null;

        /// <inheritdoc/>
        protected override void SetText(string text)
        {
            this.text.Text = text;
        }
    }
}
