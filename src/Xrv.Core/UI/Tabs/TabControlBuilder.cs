using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Collections.Generic;

namespace Xrv.Core.UI.Tabs
{
    public class TabControlBuilder
    {
        private readonly AssetsService assetsService;
        private Entity entity;
        private TabControl control;

        public TabControlBuilder(AssetsService assetsService)
        {
            this.assetsService = assetsService;
        }

        public TabControlBuilder Create()
        {
            var prefab = this.assetsService.Load<Prefab>(DefaultResourceIDs.Prefabs.TabControl);
            this.entity = prefab.Instantiate();
            this.control = this.entity.FindComponentInChildren<TabControl>();
            return this;
        }

        public TabControlBuilder WithSize(Vector2 size)
        {
            this.control.Size = size;
            return this;
        }

        public TabControlBuilder AddItem(TabItem item)
        {
            this.control.Items.Add(item);
            return this;
        }

        public TabControlBuilder AddItems(IEnumerable<TabItem> items)
        {
            foreach (var item in items)
            {
                this.control.Items.Add(item);
            }
            return this;
        }

        public Entity Build() => this.entity;
    }
}
