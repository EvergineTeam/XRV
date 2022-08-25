using Evergine.Framework.Managers;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Settings
{
    public class SettingsSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private HandMenuButtonDescription handMenuButtonDescription;

        public SettingsSystem(XrvService xrvService, EntityManager entityManager)
        {
            this.xrvService = xrvService;
            this.entityManager = entityManager;
        }

        public TabbedWindow Window { get; private set; }

        internal void Load()
        {
            this.Window = this.CreateSettingsWindow();
            this.SetUpHandMenu();
        }

        public void AddTabItem(TabItem item) => this.Window.Tabs.Add(item);

        public void RemoveTabItem(TabItem item) => this.Window.Tabs.Remove(item);

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
            this.handMenuButtonDescription = new HandMenuButtonDescription
            {
                IsToggle = false,
                IconOn = CoreResourcesIDs.Materials.Icons.Settings,
                TextOn = "Settings",
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
    }
}
