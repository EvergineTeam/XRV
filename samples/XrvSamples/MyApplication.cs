using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using System;
using Evergine.Xrv.AudioNotes;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.Storage.Cache;
using Evergine.Xrv.ImageGallery;
using Evergine.Xrv.ModelViewer;
using Evergine.Xrv.Painter;
using Evergine.Xrv.Ruler;
using Evergine.Xrv.StreamingViewer;
using Random = Evergine.Framework.Services.Random;
using Evergine.Common.IO;

namespace XrvSamples
{
    public partial class MyApplication : Application
    {
        public MyApplication()
        {
            this.Container.Register<Settings>();
            this.Container.Register<Clock>();
            this.Container.Register<TimerFactory>();
            this.Container.Register<Random>();
            this.Container.Register<ErrorHandler>();
            this.Container.Register<ScreenContextManager>();
            this.Container.Register<GraphicsPresenter>();
            this.Container.Register<AssetsDirectory>();
            this.Container.Register<AssetsService>();
            this.Container.Register<ForegroundTaskSchedulerService>();
            this.Container.Register<WorkActionScheduler>();

            BackgroundTaskScheduler.Background.Configure(this.Container);
        }

        public int? NetworkingClientPort { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            this.ConfigureXrv();

            // Get ScreenContextManager
            var screenContextManager = this.Container.Resolve<ScreenContextManager>();
            var assetsService = this.Container.Resolve<AssetsService>();

            // Navigate to scene
            //var scene = assetsService.Load<Scenes.HandMenuScene>(EvergineContent.Scenes.HandMenu_wescene);
            //var scene = assetsService.Load<Scenes.StorageScene>(EvergineContent.Scenes.StorageScene_wescene);
            var scene = assetsService.Load<Scenes.EmptyScene>(EvergineContent.Scenes.Empty_wescene);
            //var scene = assetsService.Load<Scenes.WindowScene>(EvergineContent.Scenes.Windows_wescene);

            ScreenContext screenContext = new ScreenContext(scene);
            screenContextManager.To(screenContext);
        }

        private void ConfigureXrv()
        {
            var sharedFileUri = new Uri("https://xrvdevelopment.file.core.windows.net/samples?sv=2021-10-04&st=2023-05-10T11%3A25%3A13Z&se=2080-05-11T11%3A25%3A00Z&sr=s&sp=rl&sig=%2F0XeYMPfPfRf1zLQvbu97RZgaPoX9NBqnaLruuQygY4%3D");

            // Repositories
            var loadModelFileAccess = AzureFileShareFileAccess.CreateFromUri(sharedFileUri);
            loadModelFileAccess.Cache = new DiskCache("models");
            loadModelFileAccess.BaseDirectory = "models";

            var imageGalleryFileAccess = AzureFileShareFileAccess.CreateFromUri(sharedFileUri);
            imageGalleryFileAccess.Cache = new DiskCache("images");
            imageGalleryFileAccess.BaseDirectory = "images";

            // XRV service
            var xrv = new XrvService()
                .AddModule(new RulerModule())
                .AddModule(new ModelViewerModule()
                {
                    Repositories = new Repository[]
                                    {
                                        new Repository()
                                        {
                                            Name = "Remote Sample Models",
                                            FileAccess = loadModelFileAccess,
                                        }
                                    },
                    NormalizedModelEnabled = true,
                    NormalizedModelSize = 0.2f,
                })
                .AddModule(new AudioNotesModule())
                .AddModule(new ImageGalleryModule()
                {
                    ImagePixelsWidth = 640,
                    ImagePixelsHeight = 640,
                    FileAccess = imageGalleryFileAccess,
                })
                .AddModule(new StreamingViewerModule()
                {
                    SourceURL = "http://94.124.210.59:8083/mjpg/video.mjpg"
                })
                .AddModule(new PainterModule());


            this.Container.RegisterInstance(xrv);
        }
    }
}
