// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;

namespace Evergine.Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Text style component for standard buttons.
    /// </summary>
    public class ButtonTextStyle : BaseTextStyleComponent
    {
        [BindComponent]
        private StandardButtonConfigurator configurator = null;

        /// <inheritdoc/>
        protected override void ApplyTextStyle(TextStyle style)
        {
            var font = this.GetPreferredFont(style);
            if (font != null)
            {
                this.configurator.Font = font;
            }

            this.configurator.TextScale = style.TextScale;

            var color = this.GetPreferredColor(style);
            if (color.HasValue)
            {
                this.configurator.PrimaryColor = color.Value;
            }
        }
    }
}
