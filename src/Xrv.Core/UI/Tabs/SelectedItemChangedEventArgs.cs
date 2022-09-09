// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.UI.Tabs
{
    /// <summary>
    /// Event arguments for tab item selection changes.
    /// </summary>
    public class SelectedItemChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedItemChangedEventArgs"/> class.
        /// </summary>
        /// <param name="item">Selected tab item.</param>
        public SelectedItemChangedEventArgs(TabItem item)
        {
            this.Item = item;
        }

        /// <summary>
        /// Gets selected tab item.
        /// </summary>
        public TabItem Item { get; private set; }
    }
}
