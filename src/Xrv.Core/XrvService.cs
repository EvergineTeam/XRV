﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Components.Animation;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Help;
using Xrv.Core.Menu;
using Xrv.Core.Messaging;
using Xrv.Core.Modules;
using Xrv.Core.Networking;
using Xrv.Core.Services;
using Xrv.Core.Services.QR;
using Xrv.Core.Settings;
using Xrv.Core.Themes;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
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
        private Entity handTutorialRootEntity;

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
        /// Gets access to network system.
        /// </summary>
        public NetworkSystem Networking { get; private set; }

        /// <summary>
        /// Gets basic publisher-subscriber implementation.
        /// </summary>
        public PubSub PubSub { get; private set; }

        /// <summary>
        /// Gets access to settings system.
        /// </summary>
        public SettingsSystem Settings { get; private set; }

        /// <summary>
        /// Gets cross-cutting services.
        /// </summary>
        public CrossCutting Services { get; private set; }

        /// <summary>
        /// Gets window system access.
        /// </summary>
        public WindowsSystem WindowSystem { get; private set; }

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
            this.Services = new CrossCutting();
            this.Services.QrScanningFlow = new QrScanningFlow(
                scene.Managers.EntityManager,
                scene.Managers.RenderManager,
                this.assetsService);

            // Themes
            this.ThemesSystem = new ThemesSystem(this.assetsService, this.graphicsContext);
            this.ThemesSystem.Load();

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
            TabControl.Builder = new TabControlBuilder(this, this.assetsService);
            this.Help = new HelpSystem(this, scene.Managers.EntityManager);
            this.Help.Load();
            this.Settings = new SettingsSystem(this, this.assetsService, scene.Managers.EntityManager);
            this.Settings.Load();

            // Voice commands
            this.voiceSystem.Load();

            this.Networking = new NetworkSystem(this, this.assetsService);
            this.Networking.RegisterServices();
            this.Networking.Load();

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

            // Add Hand tutorial to the scene
            this.handTutorialRootEntity = this.CreateHandTutorial(scene);
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

        private Entity CreateHandTutorial(Scene scene)
        {
            // Load handtutorial model
            var handTutorialModel = this.assetsService.Load<Model>(CoreResourcesIDs.Models.Hand_Panel_anim_glb);
            var handTutorialEntity = handTutorialModel.InstantiateModelHierarchy(this.assetsService);
            handTutorialEntity.FindComponent<Transform3D>().Position = Vector3.Down * 0.3f;
            handTutorialEntity.FindComponent<Animation3D>().PlayAutomatically = true;
            var handMesh = handTutorialEntity.Find("[this].L_Hand.MeshL");
            handMesh.FindComponent<MaterialComponent>().Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.HandTutorial);
            handMesh.FindComponent<SkinnedMeshRenderer>().UseComputeSkinning = DeviceInfo.PlatformType == Evergine.Common.PlatformType.UWP ? false : true;
            var panelMesh = handTutorialEntity.Find("[this].Panel");
            panelMesh.FindComponent<MaterialComponent>().Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.PrimaryColor2);

            // Root with Tagalong
            Entity root = new Entity()
               .AddComponent(new Transform3D())
               .AddComponent(new WindowTagAlong()
               {
                   MaxHAngle = MathHelper.ToRadians(15),
                   MaxVAngle = MathHelper.ToRadians(45),
                   MaxLookAtAngle = MathHelper.ToRadians(15),
                   MinDistance = 0.4f,
                   MaxDistance = 0.6f,
                   DisableVerticalLookAt = false,
                   MaxVerticalDistance = 0.1f,
                   SmoothPositionFactor = 0.01f,
                   SmoothOrientationFactor = 0.05f,
                   SmoothDistanceFactor = 1.2f,
               });

            root.AddChild(handTutorialEntity);

            scene.Managers.EntityManager.Add(root);
            this.HandMenu.PalmUpDetected += this.HandMenu_PalmUpDetected;

            return root;
        }

        private void HandMenu_PalmUpDetected(object sender, EventArgs e)
        {
            this.handTutorialRootEntity.IsEnabled = false;
            this.HandMenu.PalmUpDetected -= this.HandMenu_PalmUpDetected;
        }
    }
}
