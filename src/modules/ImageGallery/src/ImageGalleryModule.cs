using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
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
        private TabItem settings;
        private TabItem help;
        private Entity imageGalleryHelp;
        private Entity imageGallerySettings;
        private Scene scene;
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

            ////this.settings = new TabItem()
            ////{
            ////    Name = "Image Gallery",
            ////    Contents = this.SettingContent,
            ////};

            ////this.help = new TabItem()
            ////{
            ////    Name = "Image Gallery",
            ////    Contents = this.HelpContent,
            ////};
        }

        /// <inheritdoc/>
        public override string Name => "Image Gallery";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDescription;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => this.settings;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            var gallery = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Gallery).Instantiate();

            var size = new Vector2(0.30f, 0.30f);

            this.window = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.Title = "Gallery";
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
            this.SetFrontPosition(this.scene, this.window.Owner);
            this.window.Open();
        }

        private void SetFrontPosition(Scene scene, Entity entity)
        {
            var entityTransform = entity.FindComponent<Transform3D>();
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            entityTransform.Position = cameraTransform.Position + (cameraWorldTransform.Forward * this.xrv.WindowSystem.Distances.Near);
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
