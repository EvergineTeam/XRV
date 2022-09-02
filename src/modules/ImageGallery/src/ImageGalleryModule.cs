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

namespace Xrv.ImageGallery
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class ImageGalleryModule : Module
    {
        /// <summary>
        /// Name of the module.
        /// </summary>
        public override string Name => "Image Gallery";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDescription;

        public override TabItem Help => this.help;

        public override TabItem Settings => this.settings;

        protected AssetsService assetsService;
        private XrvService xrv;
        private HandMenuButtonDescription handMenuDescription;
        private TabItem settings;
        private TabItem help;

        private Entity imageGalleryHelp;
        private Entity imageGallerySettings;
        private Scene scene;

        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private Window window = null;
        private bool isWindowConfigured = false;

        public ImageGalleryModule()
        {
            this.handMenuDescription = new HandMenuButtonDescription()
            {
                IconOn = ImageGalleryResourceIDs.Materials.Icons.ImageGallery,
                IsToggle = false,
                TextOn = "Image Gallery",
            };

            this.settings = new TabItem()
            {
                Name = "Image Gallery",
                Contents = this.SettingContent,
            };

            this.help = new TabItem()
            {
                Name = "Image Gallery",
                Contents = this.HelpContent,
            };
        }

        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            this.window = this.xrv.WindowSystem.ShowWindow();
        }

        public override void Run(bool turnOn)
        {
            if (!isWindowConfigured)
            {
                var gallery = this.assetsService.Load<Prefab>(ImageGalleryResourceIDs.Prefabs.Gallery).Instantiate();
                var config = this.window.Configurator;
                var size = new Vector2(0.30f, 0.30f);
                config.Title = "Gallery";
                config.Size = size;
                config.FrontPlateSize = size;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = gallery;
            }

            this.SetFrontPosition(this.scene, this.window.Owner);
            this.window.Open();
        }

        public Vector3 GetFrontPosition(Scene scene)
        {
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            //// TODO uses NEAR position instead of 0.6f
            return cameraTransform.Position + (cameraWorldTransform.Forward * 0.6f);
        }

        public void SetFrontPosition(Scene scene, Entity entity)
        {
            var windowTransform = entity.FindComponent<Transform3D>();
            windowTransform.Position = this.GetFrontPosition(scene);
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
