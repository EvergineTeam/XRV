// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;

namespace Xrv.Core.UI.Tabs
{
    public class TabItem
    {
        public TabItem()
        {
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public string Name { get; set; }

        public object Data { get; set; }

        public Func<Entity> Contents { get; set; }
    }
}
