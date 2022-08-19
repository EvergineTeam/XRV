using Evergine.Framework.Managers;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Help
{
    public class HelpSystem
    {
        private readonly EntityManager entityManager;
        private readonly XrvService xrvService;
        private HandMenuButtonDescription handMenuButtonDescription;

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
            var configurator = owner.FindComponent<WindowConfiguration>();
            var window = owner.FindComponent<TabbedWindow>();
            configurator.Title = "Help";

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
                IconOn = DefaultResourceIDs.Materials.Icons.Help,
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
    }
}
