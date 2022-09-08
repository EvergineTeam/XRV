// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Help;
using Xrv.Core.Menu;
using Xrv.Core.Messaging;
using Xrv.Core.Modules;
using Xrv.Core.Settings;
using Xrv.Core.UI.Tabs;
using Xrv.Core.VoiceCommands;
using WindowsSystem = Xrv.Core.UI.Windows.WindowsSystem;

namespace Xrv.Core
{
    /// <summary>
    /// XRV framework service.
    /// </summary>
    public class XrvService : Service
    {
        private readonly Dictionary<Type, Module> modules;
        private readonly VoiceCommandsSystem voiceSystem = null;

        [BindService]
        private AssetsService assetsService = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="XrvService"/> class.
        /// </summary>
        public XrvService()
        {
            this.modules = new Dictionary<Type, Module>();
            this.PubSub = new PubSub();
            this.PubSub.Subscribe<ActivateModuleMessage>(message =>
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
        public HelpSystem Help { get; private set; }

        /// <summary>
        /// Gets basic publisher-subscriber implementation.
        /// </summary>
        public PubSub PubSub { get; private set; }

        /// <summary>
        /// Gets access to settings system.
        /// </summary>
        public SettingsSystem Settings { get; private set; }

        /// <summary>
        /// Gets window system access.
        /// </summary>
        public WindowsSystem WindowSystem { get; private set; }

        /// <summary>
        /// Adds a module to module system. Module will be initalized on scene initialization.
        /// </summary>
        /// <param name="module">Module instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when supplied module is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when trying to register a module twice.</exception>
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
            // Clear camera background
            var camera = scene.Managers.EntityManager.FindComponentsOfType<Camera3D>().First();
            camera.BackgroundColor = Color.Transparent;

            // Register services and managers
            this.WindowSystem = new WindowsSystem(scene.Managers.EntityManager, this.assetsService);
            this.WindowSystem.Load();

            // Hand menu initialization
            var handMenuManager = new HandMenuManager(scene.Managers.EntityManager, this.assetsService);
            this.HandMenu = handMenuManager.Initialize();

            // Add controls and systems
            TabControl.Builder = new TabControlBuilder(this.assetsService);
            this.Help = new HelpSystem(this, scene.Managers.EntityManager);
            this.Help.Load();
            this.Settings = new SettingsSystem(this, this.assetsService, scene.Managers.EntityManager);
            this.Settings.Load();

            // Voice commands
            this.voiceSystem.Load();

            foreach (var module in this.modules.Values)
            {
                // Adding hand menu button for module, if any
                if (module.HandMenuButton != null)
                {
                    this.HandMenu.ButtonDescriptions.Add(module.HandMenuButton);
                }

                // Adding setting data
                if (module.Help != null)
                {
                    this.Help.AddTabItem(module.Help);
                }

                // Adding setting data
                if (module.Settings != null)
                {
                    this.Settings.AddTabItem(module.Settings);
                }

                // Voice commands
                var voiceCommands = module.VoiceCommands;
                if (voiceCommands?.Any() == true)
                {
                    this.voiceSystem.RegisterCommands(voiceCommands);
                }

                // Modules initialization
                module.Initialize(scene);
            }

            // Initialize voice commands (after collecting keywords)
            this.voiceSystem.Initialize();
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
