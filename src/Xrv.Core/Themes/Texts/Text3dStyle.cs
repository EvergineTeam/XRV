// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;

namespace Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Text style component for <see cref="Text3DMesh"/>.
    /// </summary>
    public class Text3dStyle : BaseTextStyleComponent
    {
        [BindComponent]
        private Text3DMesh text3d = null;

        /// <inheritdoc/>
        protected override void ApplyTextStyle(TextStyle style)
        {
            var font = this.GetPreferredFont(style);
            if (font != null)
            {
                this.text3d.Font = font;
            }

            this.text3d.ScaleFactor = style.TextScale;

            var color = this.GetPreferredColor(style);
            if (color.HasValue)
            {
                this.text3d.Color = color.Value;
            }
        }
    }
}
