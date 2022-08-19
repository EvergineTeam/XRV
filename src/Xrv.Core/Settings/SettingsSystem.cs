using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Mathematics;
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

        public SettingsWindow Window { get; private set; }

        internal void Load()
        {
            this.Window = this.CreateSettingsWindow();
            this.SetUpHandMenu();
        }

        private SettingsWindow CreateSettingsWindow()
        {
            var owner = this.xrvService.WindowSystem.BuildWindow(new SettingsWindow(), (BaseWindowConfigurator)null);
            var window = owner.FindComponent<SettingsWindow>();
            var configurator = owner.FindComponent<WindowConfiguration>();
            configurator.Title = "Settings";
            configurator.DisplayFrontPlate = false;
            configurator.Size = new Vector2(0.26f, 0.16f);
            configurator.FrontPlateOffsets = Vector2.Zero;

            var contents = TabControl.Builder
                .Create()
                .WithSize(new Vector2(0.24f, 0.16f))
                .Build();
            var tabControl = contents.FindComponent<TabControl>();
            tabControl.DestroyContentOnTabChange = false;
            configurator.Content = contents;

            var transform = contents.FindComponent<Transform3D>();
            var position = transform.LocalPosition;
            position.X += 0.035f;
            transform.LocalPosition = position;

            owner.IsEnabled = false;
            this.entityManager.Add(owner);

            window.Sections.Add(new Section
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
                IconOn = DefaultResourceIDs.Materials.Settings,
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
