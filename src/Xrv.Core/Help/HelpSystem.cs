// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Help
{
    public class HelpSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private MenuButtonDescription handMenuButtonDescription;

        private Entity generalHelp;
        private Entity about;

        public HelpSystem(XrvService xrvService, EntityManager entityManager)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
        }

        public TabbedWindow Window { get; private set; }

        internal void Load()
        {
            this.Window = this.CreateHelpWindow();
            this.SetUpHandMenu();
        }

        public void AddTabItem(TabItem item) => this.Window.Tabs.Add(item);

        public void RemoveTabItem(TabItem item) => this.Window.Tabs.Remove(item);

        private TabbedWindow CreateHelpWindow()
        {
            var owner = TabbedWindow.Create(this.xrvService);
            var configurator = owner.FindComponent<WindowConfigurator>();
            var window = owner.FindComponent<TabbedWindow>();
            configurator.Title = "Help";

            owner.IsEnabled = false;
            this.entityManager.Add(owner);

            window.Tabs.Add(new TabItem
            {
                Name = "General",
                Contents = this.GeneralHelp,
            });

            window.Tabs.Add(new TabItem
            {
                Name = "About",
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
                TextOn = "Help",
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
    }
}
