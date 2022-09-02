// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System.Collections.Generic;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.UI.Tabs
{
    public class TabbedWindow : Window
    {
        private TabControl tabControl;
        private List<TabItem> tabsToBeLoadedWhenAvailable;

        public TabbedWindow()
        {
            this.tabsToBeLoadedWhenAvailable = new List<TabItem>();
        }

        public IList<TabItem> Tabs { get => this.tabControl?.Items ?? this.tabsToBeLoadedWhenAvailable; }

        public static Entity Create(XrvService xrvService)
        {
            var owner = xrvService.WindowSystem.BuildWindow(new TabbedWindow(), (BaseWindowConfigurator)null);
            var configurator = owner.FindComponent<WindowConfigurator>();
            configurator.DisplayFrontPlate = false;
            configurator.Size = new Vector2(0.35f, 0.2f);
            configurator.FrontPlateOffsets = Vector2.Zero;

            var contents = TabControl.Builder
                .Create()
                .WithSize(new Vector2(0.315f, 0.2f))
                .Build();
            var tabControl = contents.FindComponent<TabControl>();
            tabControl.DestroyContentOnTabChange = false;
            configurator.Content = contents;

            var transform = contents.FindComponent<Transform3D>();
            var position = transform.LocalPosition;
            position.X += 0.035f;
            transform.LocalPosition = position;

            return owner;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.tabControl == null)
            {
                this.tabControl = this.Owner.FindComponentInChildren<TabControl>();

                this.tabControl.Items.Clear();
                foreach (var item in this.tabsToBeLoadedWhenAvailable)
                {
                    this.tabControl.Items.Add(item);
                }

                this.tabsToBeLoadedWhenAvailable.Clear();
            }
        }
    }
}
