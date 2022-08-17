using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.Themes;

namespace Xrv.Core
{
    public class XrvService : Service
    {
        [BindService]
        private AssetsService assetsService = null;

        private readonly Dictionary<Type, Module> modules;

        public HandMenu HandMenu { get; private set; }

        public Theme CurrentTheme { get; set; }

        public XrvService()
        {
            this.modules = new Dictionary<Type, Module>();
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

        internal Module GetModuleForHandButton(HandMenuButtonDefinition definition)
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
            scene.Managers.AddManager(new UI.Windows.WindowManager());

            // Hand menu initialization
            var handMenuManager = new HandMenuManager(scene.Managers.EntityManager, this.assetsService);
            this.HandMenu = handMenuManager.Initialize();

            foreach(var module in this.modules.Values)
            {
                // Adding hand menu button for module, if any
                if (module.HandMenuButton != null)
                {
                    this.HandMenu.ButtonDefinitions.Add(module.HandMenuButton);
                }

                // Modules initialization
                module.Initialize(scene);
            }
        }
    }
}
