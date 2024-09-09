// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.UI.Buttons;
using System;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Event arguments for action button pressing.
    /// </summary>
    public class ActionButtonPressedEventArgs(ButtonDescription description, bool isOn)
        : EventArgs
    {
        /// <summary>
        /// Gets associated button description.
        /// </summary>
        public ButtonDescription Description { get; private set; } = description;

        /// <summary>
        /// Gets a value indicating whether toggle state value (for toggle buttons only).
        /// </summary>
        public bool IsOn { get; private set; } = isOn;
    }
}
