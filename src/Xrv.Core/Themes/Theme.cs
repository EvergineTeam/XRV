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
            var theme = new Theme
            {
                Name = "Default",
            };
            SetDefaultValues(theme);
            return theme;
        });

        private readonly Dictionary<ThemeColor, Color> colors;
        private readonly Dictionary<ThemeFont, Guid> fonts;

        /// <summary>
        /// Initializes a new instance of the <see cref="Theme"/> class.
        /// </summary>
        public Theme()
        {
            this.colors = new Dictionary<ThemeColor, Color>();
            this.fonts = new Dictionary<ThemeFont, Guid>();
            SetDefaultValues(this);
        }

        internal event EventHandler<ThemeColor> ColorChanged;

        internal event EventHandler<ThemeFont> FontChanged;

        /// <summary>
        /// Gets or sets theme name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets color used for:
        /// - Windows title bar plate.
        /// - Text color for buttons with light colors.
        /// - Lists: selected item background color.
        /// </summary>
        public Color PrimaryColor1
        {
            get => this.colors[ThemeColor.PrimaryColor1];
            set => this.SetColor(ThemeColor.PrimaryColor1, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Lists: scroll bar color.
        /// </summary>
        public Color PrimaryColor2
        {
            get => this.colors[ThemeColor.PrimaryColor2];
            set => this.SetColor(ThemeColor.PrimaryColor2, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Global text color.
        /// - Text color for selected items in a tab control.
        /// </summary>
        public Color PrimaryColor3
        {
            get => this.colors[ThemeColor.PrimaryColor3];
            set => this.SetColor(ThemeColor.PrimaryColor3, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Text color for unselected items in a tab control.
        /// - Backplate color for some buttons.
        /// - Switch buttons: on state.
        /// - Lists: text color for selected item.
        /// - In general, to mark manipulation items as selected.
        /// </summary>
        public Color SecondaryColor1
        {
            get => this.colors[ThemeColor.SecondaryColor1];
            set => this.SetColor(ThemeColor.SecondaryColor1, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Manipulators color.
        /// </summary>
        public Color SecondaryColor2
        {
            get => this.colors[ThemeColor.SecondaryColor2];
            set => this.SetColor(ThemeColor.SecondaryColor2, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Switch buttons: off state.
        /// - Confirm dialogs: accept option backplate.
        /// </summary>
        public Color SecondaryColor3
        {
            get => this.colors[ThemeColor.SecondaryColor3];
            set => this.SetColor(ThemeColor.SecondaryColor3, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Windows front plate gradient start.
        /// </summary>
        public Color SecondaryColor4
        {
            get => this.colors[ThemeColor.SecondaryColor4];
            set => this.SetColor(ThemeColor.SecondaryColor4, value);
        }

        /// <summary>
        /// Gets or sets color used for:
        /// - Windows front plate gradient end.
        /// </summary>
        public Color SecondaryColor5
        {
            get => this.colors[ThemeColor.SecondaryColor5];
            set => this.SetColor(ThemeColor.SecondaryColor5, value);
        }

        /// <summary>
        /// Gets or sets font used for:
        /// - Window titles.
        /// - Tab items.
        /// - Content section labels.
        /// - Buttons text.
        /// </summary>
        public Guid PrimaryFont1
        {
            get => this.fonts[ThemeFont.PrimaryFont1];
            set => this.SetFont(ThemeFont.PrimaryFont1, value);
        }

        /// <summary>
        /// Gets or sets font used for:
        /// - Content texts.
        /// - Hand menu buttons text.
        /// - Window buttons text.
        /// </summary>
        public Guid PrimaryFont2
        {
            get => this.fonts[ThemeFont.PrimaryFont2];
            set => this.SetFont(ThemeFont.PrimaryFont2, value);
        }

        /// <summary>
        /// Gets color value by a themed color.
        /// </summary>
        /// <param name="color">Themed color.</param>
        /// <returns>Color.</returns>
        public Color GetColor(ThemeColor color) => this.colors[color];

        /// <summary>
        /// Gets font identifier by a themed font.
        /// </summary>
        /// <param name="font">Themed font.</param>
        /// <returns>Font identifier.</returns>
        public Guid GetFont(ThemeFont font) => this.fonts[font];

        private static void SetDefaultValues(Theme theme)
        {
            theme.PrimaryColor1 = Color.FromHex("#041C2C");
            theme.PrimaryColor2 = Color.FromHex("#00B5F1");
            theme.PrimaryColor3 = Color.FromHex("#EBEBEB");
            theme.SecondaryColor1 = Color.FromHex("#70F2F8");
            theme.SecondaryColor2 = Color.FromHex("#62CCD5");
            theme.SecondaryColor3 = Color.FromHex("#F10A42");
            theme.SecondaryColor4 = Color.FromHex("#0F72E8");
            theme.SecondaryColor5 = Color.FromHex("#552098");
            theme.PrimaryFont1 = CoreResourcesIDs.Fonts.Montserrat_SemiBold;
            theme.PrimaryFont2 = CoreResourcesIDs.Fonts.Montserrat_Regular;
        }

        private void SetColor(ThemeColor color, Color @value)
        {
            if (this.colors.ContainsKey(color) && this.colors[color] == @value)
            {
                return;
            }

            this.colors[color] = @value;
            this.ColorChanged?.Invoke(this, color);
        }

        private void SetFont(ThemeFont font, Guid @value)
        {
            if (this.fonts.ContainsKey(font) && this.fonts[font] == @value)
            {
                return;
            }

            this.fonts[font] = @value;
            this.FontChanged?.Invoke(this, font);
        }
    }
}
