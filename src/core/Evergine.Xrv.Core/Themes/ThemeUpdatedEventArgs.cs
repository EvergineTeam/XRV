// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Themes
{
    /// <summary>
    /// Theme update event arguments.
    /// </summary>
    public class ThemeUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets updated theme instance.
        /// </summary>
        public Theme Theme { get; set; }

        /// <summary>
        /// Gets or sets theme updated color key.
        /// </summary>
        public ThemeColor? UpdatedColor { get; set; }

        /// <summary>
        /// Gets a value indicating whether event is produced by a global theme change.
        /// </summary>
        public bool IsNewThemeInstance { get => !this.UpdatedColor.HasValue; }
    }
}
