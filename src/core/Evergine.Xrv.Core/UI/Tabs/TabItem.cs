// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;

namespace Evergine.Xrv.Core.UI.Tabs
{
    /// <summary>
    /// Tab item model.
    /// </summary>
    public class TabItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabItem"/> class.
        /// </summary>
        public TabItem()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets tab identifier.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets tab name.
        /// </summary>
        public Func<string> Name { get; set; }

        /// <summary>
        /// Gets or sets tab data.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets tab order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets a function to create tab content instance.
        /// </summary>
        public Func<Entity> Contents { get; set; }
    }
}
