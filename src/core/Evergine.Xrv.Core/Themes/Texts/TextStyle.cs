// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using System;

namespace Evergine.Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Text style definition.
    /// </summary>
    public class TextStyle
    {
        /// <summary>
        /// Gets or sets font asset identifier.
        /// </summary>
        public Guid? Font { get; set; }

        /// <summary>
        /// Gets or sets themed font.
        /// </summary>
        public ThemeFont? ThemeFont { get; set; }

        /// <summary>
        /// Gets or sets text scale.
        /// </summary>
        public float TextScale { get; set; }

        /// <summary>
        /// Gets or sets text color.
        /// </summary>
        public Color? TextColor { get; set; }

        /// <summary>
        /// Gets or sets text themed color.
        /// </summary>
        public ThemeColor? ThemeColor { get; set; }
    }
}
