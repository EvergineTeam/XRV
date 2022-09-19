// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using Evergine.Common.Graphics;
using System;
using System.Collections.Generic;

namespace Xrv.Core.Themes
{
    /// <summary>
    /// Theme definition.
    /// </summary>
    public class Theme
    {
        /// <summary>
        /// Default theme.
        /// </summary>
        public static Lazy<Theme> Default = new Lazy<Theme>(() =>
        {
            var theme = new Theme();
            SetDefaultValues(theme);
            return theme;
        });

        private readonly Dictionary<ThemeColor, Color> colors;

        /// <summary>
        /// Initializes a new instance of the <see cref="Theme"/> class.
        /// </summary>
        public Theme()
        {
            this.colors = new Dictionary<ThemeColor, Color>();
            SetDefaultValues(this);
        }

        internal event EventHandler<ThemeColor> ColorChanged;

        /// <summary>
        /// Gets or sets theme name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a theme color.
        /// </summary>
        /// <param name="color">Color key.</param>
        /// <returns>Theme color.</returns>
        public Color this[ThemeColor color]
        {
            get => this.GetColor(color);
            set => this.SetColor(color, value);
        }

        private static void SetDefaultValues(Theme theme)
        {
            theme[ThemeColor.PrimaryColor1] = Color.FromHex("#041C2C");
            theme[ThemeColor.PrimaryColor2] = Color.FromHex("#00B5F1");
            theme[ThemeColor.PrimaryColor3] = Color.FromHex("#EBEBEB");
            theme[ThemeColor.SecondaryColor1] = Color.FromHex("#70F2F8");
            theme[ThemeColor.SecondaryColor2] = Color.FromHex("#62CCD5");
            theme[ThemeColor.SecondaryColor3] = Color.FromHex("#F10A42");
            theme[ThemeColor.SecondaryColor4] = Color.FromHex("#0F72E8");
            theme[ThemeColor.SecondaryColor5] = Color.FromHex("#552098");
        }

        private Color GetColor(ThemeColor color) => this.colors.ContainsKey(color) ? this.colors[color] : default;

        private void SetColor(ThemeColor color, Color @value)
        {
            if (this.colors.ContainsKey(color) && this.colors[color] == @value)
            {
                return;
            }

            this.colors[color] = @value;
            this.ColorChanged?.Invoke(this, color);
        }
    }
}
