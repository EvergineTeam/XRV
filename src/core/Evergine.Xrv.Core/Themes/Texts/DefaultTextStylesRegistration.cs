// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace Evergine.Xrv.Core.Themes.Texts
{
    internal class DefaultTextStylesRegistration : ITextStyleRegistration
    {
        public void Register(Dictionary<string, TextStyle> registrations)
        {
            registrations[DefaultTextStyles.XrvPrimary1Size1] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont1,
                TextScale = 0.012f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary1Size2] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont1,
                TextScale = 0.01f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary1Size3] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont1,
                TextScale = 0.008f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary2Size1] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont2,
                TextScale = 0.007f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary2Size2] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont2,
                TextScale = 0.006f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary2Size3] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont2,
                TextScale = 0.005f,
                ThemeColor = ThemeColor.PrimaryColor3,
            };

            registrations[DefaultTextStyles.XrvPrimary2Size3Alt] = new TextStyle
            {
                ThemeFont = ThemeFont.PrimaryFont2,
                TextScale = 0.005f,
                ThemeColor = ThemeColor.PrimaryColor1,
            };
        }
    }
}
