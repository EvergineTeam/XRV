// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Collections.Generic;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.Storage;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.ImageGallery
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class ImageGalleryModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private MenuButtonDescription handMenuDescription;
        private TabItem settings = null;
        private TabItem help = null;
        private Entity imageGalleryHelp;
        private Entity imageGallerySettings;
        private Window window = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageGalleryModule"/> class.
        /// Image Gallery module for navigating between different images.
        /// </summary>
        public ImageGalleryModule()
        {
            this.handMenuDescription = new MenuButtonDescription()
            {
                IconOn = ImageGalleryResourceIDs.Materials.Icons.ImageGallery,
                IsToggle = false,
                TextOn = "Image Gallery",
            };

            this.help = new TabItem()
            {
                Name = "Image Gallery",
                Contents = this.HelpContent,
            };
        }

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
        public Core.Storage.FileAccess FileAccess { get; set; }

        /// <inheritdoc/>
        public override string Name => "Image Gallery";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDescription;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => this.settings;

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public async override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            // Connecting to azure and setting cache
            if (this.FileAccess.IsCachingEnabled)
            {
                await this.FileAccess.Cache.InitializeAsync();
            }

            // Loading and setting Gallery Asset
            var gallery = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Gallery).Instantiate();
            var imageGallery = gallery.FindComponent<ImageGallery.Components.ImageGallery>();
            imageGallery.FileAccess = this.FileAccess;

            imageGallery.ImageUpdated += this.ImageGalleryImageUpdated;
            var galleryImageFrame = gallery.FindComponentInChildren<PlaneMesh>(tag: "PART_image_gallery_picture");
            var controllersTransform = gallery.FindComponentInChildren<Transform3D>(tag: "PART_image_gallery_controllers");
            imageGallery.ImagePixelsHeight = this.ImagePixelsHeight;
            imageGallery.ImagePixelsWidth = this.ImagePixelsWidth;
            var size = new Vector2(this.ImagePixelsWidth / 2000f, this.ImagePixelsHeight / 2000f);
            galleryImageFrame.Width = size.X;
            galleryImageFrame.Height = size.Y;
            controllersTransform.LocalPosition = new Vector3(0f, -(0.02f + (size.Y / 2)), 0f);

            // Setting Window
            this.window = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.Title = this.Name;
                config.Size = size;
                config.FrontPlateSize = size;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = gallery;
            });
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.window.Open();
        }

        private void ImageGalleryImageUpdated(object sender, string e)
        {
            this.window.Configurator.Title = this.Name + " " + e;
        }

        private Entity SettingContent()
        {
            if (this.imageGallerySettings == null)
            {
                var imageGallerySettingsPrefab = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Settings);
                this.imageGallerySettings = imageGallerySettingsPrefab.Instantiate();
            }

            return this.imageGallerySettings;
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
