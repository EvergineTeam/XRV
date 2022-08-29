// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.UI.Tabs
{
    public class SelectedItemChangedEventArgs : EventArgs
    {
        public SelectedItemChangedEventArgs(TabItem item)
        {
            this.Item = item;
        }

        public TabItem Item { get; private set; }
    }
}
