// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;
using System;

namespace Evergine.Xrv.Core.Help
{
    /// <summary>
    /// Help system, to add new entries to help panel.
    /// </summary>
    public class HelpSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private readonly LocalizationService localization;

        private ButtonDescription handMenuButtonDescription;

        private Entity generalHelp;
        private Entity about;

        private bool displayAboutSection;
        private TabItem aboutSectionItem;

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
            this.displayAboutSection = true;
        }

        /// <summary>
        /// Gets help window reference.
        /// </summary>
        public TabbedWindow Window { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether about section should be displayed.
        /// </summary>
        public bool DisplayAboutSection
        {
            get => this.displayAboutSection;

            set
            {
                if (this.displayAboutSection != value)
                {
                    this.displayAboutSection = value;
                    this.OnDisplayAboutSectionChange();
                }
            }
        }

        /// <summary>
        /// Gets or sets a function to retrieve about section text contents.
        /// </summary>
        public Func<string> AboutContents { get; set; }

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
            this.OnDisplayAboutSectionChange();
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

            this.aboutSectionItem = new TabItem
            {
                Name = () => this.localization.GetString(() => Resources.Strings.Help_Tab_About),
                Order = int.MaxValue,
                Contents = this.AboutHelp,
            };

            return window;
        }

        private void SetUpHandMenu()
        {
            this.handMenuButtonDescription = new ButtonDescription
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
                this.Window.TabControl.MaxVisibleItems = 5;
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

        private void OnDisplayAboutSectionChange()
        {
            if (this.displayAboutSection && !this.Window.Tabs.Contains(this.aboutSectionItem))
            {
                this.Window.Tabs.Add(this.aboutSectionItem);
            }
            else
            {
                this.Window.Tabs.Remove(this.aboutSectionItem);
            }
        }

        internal static class VoiceCommands
        {
            public static string ShowHelp = "Show help";
        }
    }
}
