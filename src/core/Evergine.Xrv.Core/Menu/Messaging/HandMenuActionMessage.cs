// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.Menu
{
    /// <summary>
    /// Message delivered when a hand menu button is released.
    /// </summary>
    public class HandMenuActionMessage
    {
        /// <summary>
        /// Gets or sets button description.
        /// </summary>
        public ButtonDescription Description { get; set; }
    }
}
