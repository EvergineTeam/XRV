// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System.Collections.Generic;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.UI.Tabs
{
    /// <summary>
    /// Tabbed window component.
    /// </summary>
    public class TabbedWindow : Window
    {
        private TabControl tabControl;
        private List<TabItem> tabsToBeLoadedWhenAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabbedWindow"/> class.
        /// </summary>
        public TabbedWindow()
        {
            this.tabsToBeLoadedWhenAvailable = new List<TabItem>();
        }

        /// <summary>
        /// Gets tab items collection.
        /// </summary>
        public IList<TabItem> Tabs { get => this.tabControl?.Items ?? this.tabsToBeLoadedWhenAvailable; }

        /// <summary>
        /// Gets window <see cref="TabControl"/> instance.
        /// </summary>
        public TabControl TabControl => this.tabControl;

        /// <summary>
        /// Creates a tabbed window instance.
        /// </summary>
        /// <param name="xrvService">XRV service instance.</param>
        /// <returns>Tabbed window.</returns>
        public static Entity Create(XrvService xrvService)
        {
            var owner = xrvService.WindowsSystem.BuildWindow(new TabbedWindow(), (BaseWindowConfigurator)null);
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
