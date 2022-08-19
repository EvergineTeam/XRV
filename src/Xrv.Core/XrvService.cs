using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Menu;
using Xrv.Core.Messaging;
using Xrv.Core.Modules;
using Xrv.Core.Settings;
using Xrv.Core.Themes;
using Xrv.Core.UI.Tabs;
using WindowsSystem = Xrv.Core.UI.Windows.WindowsSystem;

namespace Xrv.Core
{
    public class XrvService : Service
    {
        [BindService]
        private AssetsService assetsService = null;

        private readonly Dictionary<Type, Module> modules;

        public HandMenu HandMenu { get; private set; }

        public PubSub PubSub { get; private set; }

        public SettingsSystem Settings { get; private set; }

        public WindowsSystem WindowSystem { get; private set; }

        public Theme CurrentTheme { get; set; }

        public XrvService()
        {
            this.modules = new Dictionary<Type, Module>();
            this.PubSub = new PubSub();
            this.PubSub.Subscribe<ActivateModuleMessage>(message =>
            {
                message.Module.Run(message.IsOn);
            });
        }

        public void AddModule(Module module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            var type = module.GetType();
            if (this.modules.TryGetValue(type, out var _))
            {
                throw new InvalidOperationException($"Two modules with the same type {type} cannot be added");
            }

            this.modules.Add(type, module);
        }

        public T FindModule<T>() where T : Module => (T)this.FindModule(typeof(T));

        public Module FindModule(Type type) =>
            this.modules.TryGetValue(type, out var module) ? module : null;

        internal Module GetModuleForHandButton(HandMenuButtonDescription definition)
        {
            foreach (var kvp in this.modules)
            {
                if (kvp.Value.HandMenuButton == definition)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        public void Initialize(Scene scene)
        {
            // Clear camera background
            var camera = scene.Managers.EntityManager.FindComponentsOfType<Camera3D>().First();
            camera.BackgroundColor = Color.Transparent;

            // Register services and managers
            this.WindowSystem = new WindowsSystem(scene.Managers.EntityManager, this.assetsService);

            // Hand menu initialization
            var handMenuManager = new HandMenuManager(scene.Managers.EntityManager, this.assetsService);
            this.HandMenu = handMenuManager.Initialize();

            // Add controls and systems
            TabControl.Builder = new TabControlBuilder(this.assetsService);
            this.Settings = new SettingsSystem(this, scene.Managers.EntityManager);
            this.Settings.Load();

            foreach(var module in this.modules.Values)
            {
                // Adding hand menu button for module, if any
                if (module.HandMenuButton != null)
                {
                    this.HandMenu.ButtonDescriptions.Add(module.HandMenuButton);
                }

                // Modules initialization
                module.Initialize(scene);
            }
        }
    }
}
