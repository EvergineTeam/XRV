using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Mathematics;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using WindowsSystem = Xrv.Core.UI.Windows.WindowsSystem;

namespace Xrv.Core.Settings
{
    internal class SettingsLoader
    {
        private readonly EntityManager entityManager;
        private readonly WindowsSystem windowSystem;
        private readonly HandMenu handMenu;

        public SettingsLoader(EntityManager entityManager, WindowsSystem windowSystem, HandMenu handMenu)
        {
            this.entityManager = entityManager;
            this.windowSystem = windowSystem;
            this.handMenu = handMenu;
        }

        public SettingsWindow Load()
        {
            SettingsWindow window = this.CreateSettingsWindow();
            this.AddHandMenuButton();

            return window;
        }

        private SettingsWindow CreateSettingsWindow()
        {
            var owner = this.windowSystem.BuildWindow(new SettingsWindow(), (BaseWindowConfigurator)null);
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
            configurator.Content = contents;

            var transform = contents.FindComponent<Transform3D>();
            var position = transform.LocalPosition;
            position.X += 0.035f;
            transform.LocalPosition = position;

            this.entityManager.Add(owner);

            ////Evergine.Framework.Threading.EvergineForegroundTask.Run(() =>
            ////{
            ////    window.Open();
            ////});
            return window;
        }

        private void AddHandMenuButton()
        {
            this.handMenu.ButtonDescriptions.Add(new HandMenuButtonDescription
            {
                IsToggle = false,
                IconOn = DefaultResourceIDs.Materials.Settings,
                TextOn = "Settings",
            });
        }
    }
}
