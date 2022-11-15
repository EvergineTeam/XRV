// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System.Collections.Generic;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Xrv.StreamingViewer.Components;

namespace Xrv.StreamingViewer
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class StreamingViewerModule : Module
    {
        private readonly TabItem settings = null;
        private readonly TabItem help = null;
        private readonly MenuButtonDescription handMenuDescription;
        private AssetsService assetsService;
        private XrvService xrv;
        private Entity imageGalleryHelp;
        ////private Entity imageGallerySettings;
        private Scene scene;
        private Window window = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingViewerModule"/> class.
        /// Image Gallery module for navigating between different images.
        /// </summary>
        public StreamingViewerModule()
        {
            this.handMenuDescription = new MenuButtonDescription()
            {
                IconOn = StreamingViewerResourceIDs.Materials.Icons.StreamingViewer,
                IsToggle = false,
                TextOn = "Streaming Viewer",
            };

            this.help = new TabItem()
            {
                Name = "Streaming Viewer",
                Contents = this.HelpContent,
            };
        }

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public string SourceURL { get; set; }

        /// <summary>
        /// Gets or sets the width of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsHeight { get; set; }

        /// <inheritdoc/>
        public override string Name => "Streaming Viewer";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDescription;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => this.settings;

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;
            var streamingViewerPrefab = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingViewer_weprefab).Instantiate();
            var streamingViewerComponent = streamingViewerPrefab.FindComponent<StreamingViewerComponent>();
            streamingViewerComponent.SourceURL = this.SourceURL;

            // Initial size. Will be updated on stream load
            var size = new Vector2(0.30f, 0.30f);
            streamingViewerComponent.StreamingImageSizeUpdated += this.StreamImageSizeUpdated;

            this.window = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.Title = this.Name;
                config.Size = size;
                config.FrontPlateSize = size;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = streamingViewerPrefab;
            });
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.SetFrontPosition(this.scene, this.window.Owner);
            this.window.Open();
        }

        private void SetFrontPosition(Scene scene, Entity entity)
        {
            var entityTransform = entity.FindComponent<Transform3D>();
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            entityTransform.Position = cameraTransform.Position + (cameraWorldTransform.Forward * this.xrv.WindowSystem.Distances.Medium);
        }

        private void StreamImageSizeUpdated(object sender, Vector2 size)
        {
            this.window.Configurator.Size = new Vector2(size.X / 2000f, size.Y / 2000f);
            this.window.Configurator.FrontPlateSize = new Vector2(size.X / 2000f, size.Y / 2000f);
        }

        private Entity HelpContent()
        {
            if (this.imageGalleryHelp == null)
            {
                var imageGalleryHelpPrefab = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingViewerHelp_weprefab);
                this.imageGalleryHelp = imageGalleryHelpPrefab.Instantiate();
            }

            return this.imageGalleryHelp;
        }
    }
}
