// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Collections.Generic;

namespace Xrv.Core.UI.Tabs
{
    /// <summary>
    /// Builder for tab control.
    /// </summary>
    public class TabControlBuilder
    {
        private readonly XrvService xrvService;
        private readonly AssetsService assetsService;
        private Entity entity;
        private TabControl control;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabControlBuilder"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="assetsService">Assets service.</param>
        public TabControlBuilder(XrvService xrvService, AssetsService assetsService)
        {
            this.xrvService = xrvService;
            this.assetsService = assetsService;
        }

        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder Create()
        {
            var theme = this.xrvService.ThemesSystem.CurrentTheme;
            var prefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TabControl);
            this.entity = prefab.Instantiate();
            this.control = this.entity.FindComponentInChildren<TabControl>();
            this.control.ActiveItemTextColor = theme.PrimaryColor3;
            this.control.InactiveItemTextColor = theme.SecondaryColor1;

            return this;
        }

        /// <summary>
        /// Specifies a size for tab control.
        /// </summary>
        /// <param name="size">Size.</param>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder WithSize(Vector2 size)
        {
            this.control.Size = size;
            return this;
        }

        /// <summary>
        /// Adds an item to resulting tab control.
        /// </summary>
        /// <param name="item">Tab item to be added.</param>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder AddItem(TabItem item)
        {
            this.control.Items.Add(item);
            return this;
        }

        /// <summary>
        /// Adds a set of items to resulting tab control.
        /// </summary>
        /// <param name="items">Tab items to be added.</param>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder AddItems(IEnumerable<TabItem> items)
        {
            foreach (var item in items)
            {
                this.control.Items.Add(item);
            }

            return this;
        }

        /// <summary>
        /// Specifies active text color for tab item.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder WithActiveItemTextColor(Color color)
        {
            this.control.ActiveItemTextColor = color;
            return this;
        }

        /// <summary>
        /// Specifies inactive text color for tab item.
        /// </summary>
        /// <param name="color">Color.</param>
        /// <returns>Builder instance.</returns>
        public TabControlBuilder WithInactiveItemTextColor(Color color)
        {
            this.control.InactiveItemTextColor = color;
            return this;
        }

        /// <summary>
        /// Builds tab control.
        /// </summary>
        /// <returns>Tab control entity.</returns>
        public Entity Build() => this.entity;
    }
}
