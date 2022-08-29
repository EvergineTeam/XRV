// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using System;

namespace Xrv.Core.Themes
{
    public class Theme
    {
        public Color BackgroundPrimaryColor { get; } = new Color("#041C2CFF");

        public Color BackgroundSecondaryColor { get; } = new Color("#000000FF");

        public Color ForegroundPrimaryColor { get; private set; } = new Color("#115BB8FF");

        public Color ForegroundSecondaryColor { get; } = new Color("#DF4661FF");

        public Guid FontPath { get; }
    }
}
