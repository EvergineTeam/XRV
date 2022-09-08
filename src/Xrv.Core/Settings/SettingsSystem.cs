// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.Managers;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Settings
{
    /// <summary>
    /// Settings system, to add settings sections.
    /// </summary>
    public class SettingsSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private MenuButtonDescription handMenuButtonDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="entityManager">Entity manager.</param>
        public SettingsSystem(XrvService xrvService, EntityManager entityManager)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
        }

        /// <summary>
        /// Gets settings window.
        /// </summary>
        public TabbedWindow Window { get; private set; }

        /// <summary>
        /// Adds a new item to the settings tab.
        /// </summary>
        /// <param name="item">Tab item.</param>
        public void AddTabItem(TabItem item) => this.Window.Tabs.Add(item);

        /// <summary>
        /// Removes an item from the settings tab.
        /// </summary>
        /// <param name="item">Tab item.</param>
        public void RemoveTabItem(TabItem item) => this.Window.Tabs.Remove(item);

        internal void Load()
        {
            this.Window = this.CreateSettingsWindow();
            this.SetUpHandMenu();
        }

        private TabbedWindow CreateSettingsWindow()
        {
            var owner = TabbedWindow.Create(this.xrvService);
            var configurator = owner.FindComponent<WindowConfigurator>();
            var window = owner.FindComponent<TabbedWindow>();
            configurator.Title = "Settings";

            owner.IsEnabled = false;
            this.entityManager.Add(owner);

            window.Tabs.Add(new TabItem
            {
                Name = "General",
                Contents = () => null,
            });

            return window;
        }

        private void SetUpHandMenu()
        {
            this.handMenuButtonDescription = new MenuButtonDescription
            {
                IsToggle = false,
                IconOn = CoreResourcesIDs.Materials.Icons.Settings,
                TextOn = "Settings",
                VoiceCommandOn = VoiceCommands.ShowSettings,
            };
            this.xrvService.HandMenu.ButtonDescriptions.Add(this.handMenuButtonDescription);
            this.xrvService.PubSub.Subscribe<HandMenuActionMessage>(this.OnHandMenuButtonPressed);
        }

        private void OnHandMenuButtonPressed(HandMenuActionMessage message)
        {
            if (this.handMenuButtonDescription == message.Description)
            {
                this.Window.Open();
            }
        }

        internal static class VoiceCommands
        {
            public static string ShowSettings = "Show settings";
        }
    }
}
