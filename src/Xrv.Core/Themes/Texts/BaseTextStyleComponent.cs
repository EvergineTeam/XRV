// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Microsoft.Extensions.Logging;

namespace Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Base component for text styles.
    /// </summary>
    public abstract class BaseTextStyleComponent : Component
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService = null;

        /// <summary>
        /// Themes system.
        /// </summary>
        protected ThemesSystem themeSystem;

        [BindService]
        private XrvService xrvService = null;

        private ILogger logger;

        private string textStyleKey;
        private TextStyle currentStyle;

        /// <summary>
        /// Gets or sets text style key to be applied to the text.
        /// They key should have been registered by a class implementing
        /// <see cref="ITextStyleRegistration"/>.
        /// </summary>
        public string TextStyleKey
        {
            get => this.textStyleKey;

            set
            {
                if (this.textStyleKey != value)
                {
                    this.textStyleKey = value;
                    this.OnTextStyleUpdate();
                }
            }
        }

        /// <summary>
        /// Gets text color, if any is specified by referenced <see cref="TextStyle"/>.
        /// </summary>
        /// <returns>Text color defined by the style, if any.</returns>
        public Color? GetTextColor() =>
            this.currentStyle != null ? this.GetPreferredColor(this.currentStyle) : null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.themeSystem = this.xrvService.ThemesSystem;
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.OnTextStyleUpdate();
        }

        /// <summary>
        /// Applies target text style.
        /// </summary>
        /// <param name="style">Text style.</param>
        protected abstract void ApplyTextStyle(TextStyle style);

        /// <summary>
        /// Gets preferred color from text style. <see cref="TextStyle.ThemeColor"/> has
        /// preference over <see cref="TextStyle.TextColor"/>.
        /// </summary>
        /// <param name="style">Target text style.</param>
        /// <returns>Preferred color, if any is defined for given style.</returns>
        protected virtual Color? GetPreferredColor(TextStyle style)
        {
            var currentTheme = this.themeSystem?.CurrentTheme;
            Color? preferredColor = null;

            if (style.ThemeColor.HasValue && currentTheme != null)
            {
                preferredColor = currentTheme.GetColor(style.ThemeColor.Value);
            }
            else if (style.TextColor.HasValue)
            {
                preferredColor = style.TextColor.Value;
            }

            return preferredColor;
        }

        /// <summary>
        /// Gets preferred font from text style. <see cref="TextStyle.ThemeFont"/> has
        /// preference over <see cref="TextStyle.Font"/>.
        /// </summary>
        /// <param name="style">Target text style.</param>
        /// <returns>Preferred font, if any is defined for given style.</returns>
        protected virtual Font GetPreferredFont(TextStyle style)
        {
            var currentTheme = this.themeSystem?.CurrentTheme;
            Font preferredFont = null;

            if (style.ThemeFont.HasValue && currentTheme != null)
            {
                var assetId = currentTheme.GetFont(style.ThemeFont.Value);
                preferredFont = this.assetsService.Load<Font>(assetId);
            }
            else if (style.Font.HasValue)
            {
                preferredFont = this.assetsService.Load<Font>(style.Font.Value);
            }

            return preferredFont;
        }

        private void OnTextStyleUpdate()
        {
            if (!this.IsAttached || this.textStyleKey == null)
            {
                return;
            }

            var styles = TextStylesRegister.TextStyles;
            if (!styles.ContainsKey(this.textStyleKey))
            {
                this.logger?.LogWarning($"Style {this.textStyleKey} not found");
                return;
            }

            this.currentStyle = styles[this.textStyleKey];
            this.ApplyTextStyle(this.currentStyle);
        }
    }
}
