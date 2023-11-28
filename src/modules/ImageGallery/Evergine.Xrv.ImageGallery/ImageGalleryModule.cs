// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Modules.Networking;
using Evergine.Xrv.Core.Networking.Properties;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;
////using Evergine.Xrv.ImageGallery.Networking;

namespace Evergine.Xrv.ImageGallery
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class ImageGalleryModule : Module
    {
        /// <summary>
        /// Gets or sets the window.
        /// </summary>
        protected Window window;

        /// <summary>
        /// Gets or sets the window entity.
        /// </summary>
        protected Entity windowEntity;

        /// <summary>
        /// Gets or sets the image gallery.
        /// </summary>
        protected ImageGallery.Components.ImageGallery imageGallery;

        private AssetsService assetsService;
        private XrvService xrv;
        private Entity imageGalleryHelp;


        /// <summary>
        /// Gets or sets the width of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsHeight { get; set; }

        /// <summary>
        /// Gets or sets the route of the Storage used to get and store the images listed in the gallery.
        /// </summary>
        public FileAccess FileAccess { get; set; }

        /// <inheritdoc/>
        public override string Name => "Image Gallery";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;


        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            // Hand menu and help entries
            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOn = ImageGalleryResourceIDs.Materials.Icons.ImageGallery,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.Help = new TabItem()
            {
                Name = () => this.xrv.Localization.GetString(() => Resources.Strings.Help_Tab_Name),
                Contents = this.HelpContent,
            };

            // Loading and setting Gallery Asset
            var gallery = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Gallery).Instantiate();
            var imageGallery = gallery.FindComponent<Components.ImageGallery>();
            imageGallery.FileAccess = this.FileAccess;
            imageGallery.Name = this.Name;

            var galleryImageFrame = gallery.FindComponentInChildren<PlaneMesh>(tag: "PART_image_gallery_picture");
            var controllersTransform = gallery.FindComponentInChildren<Transform3D>(tag: "PART_image_gallery_controllers");
            imageGallery.ImagePixelsHeight = this.ImagePixelsHeight;
            imageGallery.ImagePixelsWidth = this.ImagePixelsWidth;
            var size = new Vector2(this.ImagePixelsWidth / 2000f, this.ImagePixelsHeight / 2000f);
            galleryImageFrame.Width = size.X;
            galleryImageFrame.Height = size.Y;
            controllersTransform.LocalPosition = new Vector3(0f, -(0.02f + (size.Y / 2)), 0f);

            // Gallery Window
            this.window = this.xrv.WindowsSystem.CreateWindow(
                out var windowEntity,
                config =>
                {
                    config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
                    config.Size = size;
                    config.DisplayFrontPlate = false;
                    config.DisplayLogo = false;
                    config.Content = gallery;
                },
                false);

            ////this.xrv.Networking.AddNetworkingEntity(windowEntity);
            windowEntity.AddComponent(new ModuleNetworkingWindowController(this));
            windowEntity.AddComponent(new TransformSynchronization());
            this.windowEntity = windowEntity;
            this.imageGallery = imageGallery;

            // Networking
            ////this.xrv.Networking.SetUpModuleSynchronization(this, new GallerySessionSynchronization(windowEntity));
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            if (turnOn)
            {
                this.window.Open();
            }
            else
            {
                this.window.Close();
            }
        }

        private Entity HelpContent()
        {
            if (this.imageGalleryHelp == null)
            {
                var imageGalleryHelpPrefab = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Help);
                this.imageGalleryHelp = imageGalleryHelpPrefab.Instantiate();
            }

            return this.imageGalleryHelp;
        }
    }
}
