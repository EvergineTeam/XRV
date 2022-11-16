using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Platform;
using Xrv.AudioNote;
using Xrv.ImageGallery;
using Xrv.Core;
using Xrv.LoadModel;
using Xrv.Ruler;
using System;
using Xrv.Core.Storage;
using Random = Evergine.Framework.Services.Random;
using Xrv.LoadModel.Structs;
using Xrv.Core.Storage.Cache;

namespace XrvSamples
{
    public partial class MyApplication : Application
    {
        public MyApplication()
        {
            this.Container.RegisterType<Settings>();
            this.Container.RegisterType<Clock>();
            this.Container.RegisterType<TimerFactory>();
            this.Container.RegisterType<Random>();
            this.Container.RegisterType<ErrorHandler>();
            this.Container.RegisterType<ScreenContextManager>();
            this.Container.RegisterType<GraphicsPresenter>();
            this.Container.RegisterType<AssetsDirectory>();
            this.Container.RegisterType<AssetsService>();
            this.Container.RegisterType<ForegroundTaskSchedulerService>();
            this.Container.RegisterType<WorkActionScheduler>();

            BackgroundTaskScheduler.Background.Configure(this.Container);            

            var xrv = new XrvService();

            // Rule module
            xrv.AddModule(new RulerModule());

            // Load Model module
            var loadModelFileAccess = AzureFileShareFileAccess.CreateFromUri(new Uri("https://waveengineagentdiag159.file.core.windows.net/models?st=2022-10-26T11%3A46%3A02Z&se=2028-10-27T18%3A46%3A00Z&sp=rl&sv=2018-03-28&sr=s&sig=dOR9IQtYCPMYfoP7TouKuh9UXjPQUMABAFLYkSbaPR0%3D"));
            ////loadModelFileAccess.Cache = new DiskCache("LoadModel");
            xrv.AddModule(new LoadModelModule()
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
            });

            // Audio Note Module
            xrv.AddModule(new AudioNoteModule());

            // Image gallery module
            var imageGalleryFileAccess = AzureFileShareFileAccess.CreateFromUri(new Uri("https://xrvgallerystorage.file.core.windows.net/galleryimages/?sv=2021-06-08&ss=f&srt=sco&sp=rwdlc&se=2024-11-03T21:21:33Z&st=2020-11-03T13:21:33Z&spr=https&sig=Xh73u%2FIVcw00vCm%2BN3z5EbyaxaIuISfCUUk0mdCiDnI%3D"));
            imageGalleryFileAccess.Cache = new DiskCache("Image Gallery Cache");
            xrv.AddModule(new ImageGalleryModule()
            {
                ImagePixelsWidth = 640,
                ImagePixelsHeight = 640,
                FileAccess = imageGalleryFileAccess,
            });

            this.Container.RegisterInstance(xrv);
        }

        public override void Initialize()
        {
            base.Initialize();

            // Get ScreenContextManager
            var screenContextManager = this.Container.Resolve<ScreenContextManager>();
            var assetsService = this.Container.Resolve<AssetsService>();

            // Navigate to scene
            //var scene = assetsService.Load<Scenes.HandMenuScene>(EvergineContent.Scenes.HandMenu_wescene);
            //var scene = assetsService.Load<Scenes.StorageScene>(EvergineContent.Scenes.StorageScene_wescene);
            var scene = assetsService.Load<Scenes.EmptyScene>(EvergineContent.Scenes.Empty_wescene);
            //var scene = assetsService.Load<Scenes.WindowScene>(EvergineContent.Scenes.Windows_wescene);
            //var scene = assetsService.Load<Scenes.ScrollsScene>(EvergineContent.Scenes.Scrolls_wescene);

            ScreenContext screenContext = new ScreenContext(scene);
            screenContextManager.To(screenContext);
        }
    }
}
