// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Linq;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Xrv.Core.Help;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Networking;
using Evergine.Xrv.Core.Services;
using Evergine.Xrv.Core.Services.Logging;
using Evergine.Xrv.Core.Services.Messaging;
using Evergine.Xrv.Core.Services.MixedReality;
using Evergine.Xrv.Core.Services.QR;
using Evergine.Xrv.Core.Settings;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.VoiceCommands;
using Microsoft.Extensions.Logging;
using WindowsSystem = Evergine.Xrv.Core.UI.Windows.WindowsSystem;

namespace Evergine.Xrv.Core
{
    /// <summary>
    /// XRV framework service.
    /// </summary>
    public class XrvService : Service
    {
        private readonly Dictionary<Type, Module> modules;
        private VoiceCommandsSystem voiceSystem;
        private ILogger logger;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        private GraphicsContext graphicsContext = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="XrvService"/> class.
        /// </summary>
        public XrvService()
        {
            this.modules = new Dictionary<Type, Module>();

            var shared = new SharedServices();
            this.Services = shared;
            this.Services.Messaging = new PubSub();
            this.Services.Messaging.Subscribe<ActivateModuleMessage>(message =>
            {
                message.Module.Run(message.IsOn);
            });

            this.voiceSystem = new VoiceCommandsSystem();
            this.voiceSystem.RegisterService();
        }

        /// <summary>
        /// Gets access to hand menu.
        /// </summary>
        public HandMenu HandMenu { get; private set; }

        /// <summary>
        /// Gets access to help system.
        /// </summary>
        public HelpSystem HelpSystem { get; private set; }

        /// <summary>
        /// Gets localization service.
        /// </summary>
        public LocalizationService Localization { get; private set; }

        /// <summary>
        /// Gets access to network system.
        /// </summary>
        public NetworkSystem Networking { get; private set; }

        /// <summary>
        /// Gets access to settings system.
        /// </summary>
        public SettingsSystem Settings { get; private set; }

        /// <summary>
        /// Gets cross-cutting services.
        /// </summary>
        public SharedServices Services { get; private set; }

        /// <summary>
        /// Gets windows system access.
        /// </summary>
        public WindowsSystem WindowsSystem { get; private set; }

        /// <summary>
        /// Gets access to themes system.
        /// </summary>
        public ThemesSystem ThemesSystem { get; private set; }

        /// <summary>
        /// Adds a module to module system. Module will be initalized on scene initialization.
        /// </summary>
        /// <param name="module">Module instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when supplied module is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to register a module twice.</exception>
        /// <returns>Return instance of myself (Fluent interface pattern).</returns>
        public XrvService AddModule(Module module)
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
            this.logger?.LogDebug($"Registered {module.Name} module");

            return this;
        }

        /// <summary>
        /// Retrieves a registered module instance from modules system.
        /// </summary>
        /// <typeparam name="T">Type of target module.</typeparam>
        /// <returns>Instace of module, if found; null otherwise.</returns>
        public T FindModule<T>()
            where T : Module => (T)this.FindModule(typeof(T));

        /// <summary>
        /// Retrieves a registered module instance from modules system.
        /// </summary>
        /// <param name="type">Type of target module.</param>
        /// <returns>Instace of module, if found; null otherwise.</returns>
        public Module FindModule(Type type) =>
            this.modules.TryGetValue(type, out var module) ? module : null;

        /// <summary>
        /// Initializes scene with XRV stuff.
        /// </summary>
        /// <param name="scene">Scene instance.</param>
        public void Initialize(Scene scene)
        {
            // Services
            this.logger?.LogDebug("Loading common services");
            this.Services.QrScanningFlow = new QrScanningFlow(
                scene.Managers.EntityManager,
                scene.Managers.RenderManager,
                this.assetsService);
            this.Services.Passthrough = new PasstroughService(scene.Managers.EntityManager);
            this.Services.Passthrough.Load();

            if (Application.Current?.IsEditor == false)
            {
                Application.Current.Container.RegisterType<LocalizationService>();
                this.Localization = Application.Current.Container.Resolve<LocalizationService>();
            }

            using (this.logger?.BeginScope("XRV initialization"))
            {
                // Themes
                this.logger?.LogDebug("Loading theme system");
                this.ThemesSystem = new ThemesSystem(this.assetsService, this.graphicsContext);
                this.ThemesSystem.Load();

                // Clear camera background
                var camera = scene.Managers.EntityManager.FindComponentsOfType<Camera3D>().First();
                camera.BackgroundColor = Color.Transparent;

                // Register services and managers
                this.logger?.LogDebug("Loading windows system");
                this.WindowsSystem = new WindowsSystem(scene.Managers.EntityManager, this.assetsService);
                this.WindowsSystem.Load();

                // Hand menu initialization
                this.logger?.LogDebug("Loading hand menu manager");
                var handMenuManager = new HandMenuManager(scene.Managers.EntityManager, this.assetsService);
                this.HandMenu = handMenuManager.Initialize();

                // Add controls and systems
                TabControl.Builder = new TabControlBuilder(this, this.assetsService);

                this.logger?.LogDebug("Loading help system");
                this.HelpSystem = new HelpSystem(this, scene.Managers.EntityManager);
                this.HelpSystem.Load();

                this.logger?.LogDebug("Loading settings system");
                this.Settings = new SettingsSystem(this, this.assetsService, scene.Managers.EntityManager);
                this.Settings.Load();

                // Voice commands
                this.logger?.LogDebug("Loading voice system");
                this.voiceSystem.Load();

                this.logger?.LogDebug("Loading networking system");
                this.Networking = new NetworkSystem(this, scene.Managers.EntityManager, this.assetsService, this.logger);
                this.Networking.RegisterServices();
                this.Networking.Load();

                foreach (var module in this.modules.Values)
                {
                    using (this.logger?.BeginScope("Module {ModuleName} initialization", module.Name))
                    {
                        // Modules initialization
                        this.logger?.LogDebug($"Initializing module");
                        module.Initialize(scene);

                        // Adding hand menu button for module, if any
                        if (module.HandMenuButton != null)
                        {
                            this.logger?.LogDebug($"Adding hand menu button");
                            this.HandMenu.ButtonDescriptions.Add(module.HandMenuButton);
                        }

                        // Adding setting data
                        if (module.Help != null)
                        {
                            this.logger?.LogDebug($"Adding help entry");
                            this.HelpSystem.AddTabItem(module.Help);
                        }

                        // Adding setting data
                        if (module.Settings != null)
                        {
                            this.logger?.LogDebug($"Adding settings entry");
                            this.Settings.AddTabItem(module.Settings);
                        }

                        // Voice commands
                        var voiceCommands = module.VoiceCommands;
                        if (voiceCommands?.Any() == true)
                        {
                            this.logger?.LogDebug($"Registering voice commands");
                            this.voiceSystem.RegisterCommands(voiceCommands);
                        }
                    }
                }

                // Initialize voice commands (after collecting keywords)
                this.logger?.LogDebug($"Initializing voice system");
                this.voiceSystem.Initialize();
            }
        }

        /// <summary>
        /// Provides logging capabilities to the framework.
        /// </summary>
        /// <param name="configuration">Logging configuration.</param>
        /// <returns>XRV service instance.</returns>
        public XrvService WithLogging(LoggingConfiguration configuration)
        {
            this.logger = new SerilogService(configuration);
            this.Services.Logging = this.logger;
            Application.Current.Container.RegisterInstance(this.logger);

            return this;
        }

        internal Module GetModuleForHandButton(MenuButtonDescription definition)
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
    }
}
