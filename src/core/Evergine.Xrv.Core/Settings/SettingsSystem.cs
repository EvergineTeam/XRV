// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.Settings
{
    /// <summary>
    /// Settings system, to add settings sections.
    /// </summary>
    public class SettingsSystem
    {
        internal const string GeneralTabData = "General";

        private readonly XrvService xrvService;
        private readonly AssetsService assetsService;
        private readonly EntityManager entityManager;

        private MenuButtonDescription handMenuButtonDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="assetsService">Assets service.</param>
        /// <param name="entityManager">Entity manager.</param>
        public SettingsSystem(XrvService xrvService, AssetsService assetsService, EntityManager entityManager)
        {
            this.xrvService = xrvService;
            this.assetsService = assetsService;
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
            configurator.LocalizedTitle = () => this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Title);

            owner.IsEnabled = false;
            this.entityManager.Add(owner);

            window.Tabs.Add(new TabItem
            {
                Name = () => this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Tab_General),
                Data = GeneralTabData,
                Contents = () => this.GetGeneralSettingsEntity(),
            });

            return window;
        }

        private void SetUpHandMenu()
        {
            this.handMenuButtonDescription = new MenuButtonDescription
            {
                IsToggle = false,
                IconOn = CoreResourcesIDs.Materials.Icons.Settings,
                TextOn = () => this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Menu),
                VoiceCommandOn = VoiceCommands.ShowSettings,
                Order = int.MaxValue - 1,
            };
            this.xrvService.HandMenu.ButtonDescriptions.Add(this.handMenuButtonDescription);
            this.xrvService.Services.Messaging.Subscribe<HandMenuActionMessage>(this.OnHandMenuButtonPressed);
        }

        private void OnHandMenuButtonPressed(HandMenuActionMessage message)
        {
            if (this.handMenuButtonDescription == message.Description)
            {
                this.Window.Open();
                this.Window.Configurator.DisplayLogo = false;
                this.Window.TabControl.MaxVisibleItems = 5;
            }
        }

        private Entity GetGeneralSettingsEntity()
        {
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.GeneralSettings_weprefab);
            return rulerSettingPrefab.Instantiate();
        }

        internal static class VoiceCommands
        {
            public static string ShowSettings = "Show settings";
        }
    }
}
