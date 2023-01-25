// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Localization;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Help
{
    /// <summary>
    /// Help system, to add new entries to help panel.
    /// </summary>
    public class HelpSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private readonly LocalizationService localization;

        private MenuButtonDescription handMenuButtonDescription;

        private Entity generalHelp;
        private Entity about;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpSystem"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service instance.</param>
        /// <param name="entityManager">Entity manager.</param>
        public HelpSystem(XrvService xrvService, EntityManager entityManager)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
            this.localization = xrvService.Localization;
        }

        /// <summary>
        /// Gets help window reference.
        /// </summary>
        public TabbedWindow Window { get; private set; }

        /// <summary>
        /// Adds a new item to the help.
        /// </summary>
        /// <param name="item">Tab item about to be added.</param>
        public void AddTabItem(TabItem item) => this.Window.Tabs.Add(item);

        /// <summary>
        /// Removes an item from the help.
        /// </summary>
        /// <param name="item">Tab item about to be removed.</param>
        public void RemoveTabItem(TabItem item) => this.Window.Tabs.Remove(item);

        internal void Load()
        {
            this.Window = this.CreateHelpWindow();
            this.SetUpHandMenu();
        }

        private TabbedWindow CreateHelpWindow()
        {
            var owner = TabbedWindow.Create(this.xrvService);
            var configurator = owner.FindComponent<WindowConfigurator>();
            var window = owner.FindComponent<TabbedWindow>();
            configurator.LocalizedTitle = () => this.localization.GetString(() => Resources.Strings.Help_Title);

            owner.IsEnabled = false;
            this.entityManager.Add(owner);

            window.Tabs.Add(new TabItem
            {
                Name = () => this.localization.GetString(() => Resources.Strings.Help_Tab_General),
                Order = int.MinValue,
                Contents = this.GeneralHelp,
            });

            window.Tabs.Add(new TabItem
            {
                Name = () => this.localization.GetString(() => Resources.Strings.Help_Tab_About),
                Order = int.MaxValue,
                Contents = this.AboutHelp,
            });

            return window;
        }

        private void SetUpHandMenu()
        {
            this.handMenuButtonDescription = new MenuButtonDescription
            {
                IsToggle = false,
                IconOn = CoreResourcesIDs.Materials.Icons.Help,
                TextOn = () => this.localization.GetString(() => Resources.Strings.Help_Title),
                VoiceCommandOn = VoiceCommands.ShowHelp,
                Order = int.MaxValue,
            };
            this.xrvService.HandMenu.ButtonDescriptions.Add(this.handMenuButtonDescription);
            this.xrvService.Services.Messaging.Subscribe<HandMenuActionMessage>(this.OnHandMenuButtonPressed);
        }

        private void OnHandMenuButtonPressed(HandMenuActionMessage message)
        {
            if (this.handMenuButtonDescription == message.Description)
            {
                this.Window.Open();
            }
        }

        private Entity GeneralHelp()
        {
            if (this.generalHelp == null)
            {
                var assetsService = Application.Current.Container.Resolve<AssetsService>();
                var generalHelpPrefab = assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.GeneralHelp_weprefab);
                this.generalHelp = generalHelpPrefab.Instantiate();
            }

            return this.generalHelp;
        }

        private Entity AboutHelp()
        {
            if (this.about == null)
            {
                var assetsService = Application.Current.Container.Resolve<AssetsService>();
                var aboutPrefab = assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.About_weprefab);
                this.about = aboutPrefab.Instantiate();
            }

            return this.about;
        }

        internal static class VoiceCommands
        {
            public static string ShowHelp = "Show help";
        }
    }
}
