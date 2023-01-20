// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using Xrv.Painter.Enums;

namespace Xrv.Painter.Helpers
{
    /// <summary>
    /// Color Helper class. Gets material from color enum.
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// Gets material from color enum.
        /// </summary>
        /// <returns>Guid of the material color.</returns>
        /// <param name="color">Color enum.</param>
        public static Guid GetMaterialFromColor(ColorEnum color)
        {
            var guid = PainterResourceIDs.Materials.Colors.WhiteColor;
            switch (color)
            {
                case ColorEnum.Blue:
                    guid = PainterResourceIDs.Materials.Colors.BlueColor;
                    break;
                case ColorEnum.BlueDark:
                    guid = PainterResourceIDs.Materials.Colors.BlueDarkColor;
                    break;
                case ColorEnum.Green:
                    guid = PainterResourceIDs.Materials.Colors.GreenColor;
                    break;
                case ColorEnum.Orange:
                    guid = PainterResourceIDs.Materials.Colors.OrangeColor;
                    break;
                case ColorEnum.Pistacho:
                    guid = PainterResourceIDs.Materials.Colors.PistachoColor;
                    break;
                case ColorEnum.Purple:
                    guid = PainterResourceIDs.Materials.Colors.PurpleColor;
                    break;
                case ColorEnum.Red:
                    guid = PainterResourceIDs.Materials.Colors.RedColor;
                    break;
                case ColorEnum.Yellow:
                    guid = PainterResourceIDs.Materials.Colors.YellowColor;
                    break;
                default:
                    break;
            }

            return guid;
        }
    }
}
