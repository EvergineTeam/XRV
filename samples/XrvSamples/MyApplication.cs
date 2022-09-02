using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Platform;
using Xrv.AudioNote;
using Xrv.ImageGallery;
using Xrv.Core;
using Xrv.LoadModel;
using Xrv.Ruler;
using Evergine.Framework.Threading;

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
            xrv.AddModule(new RulerModule());
            xrv.AddModule(new LoadModelModule());
            xrv.AddModule(new AudioNoteModule());
            xrv.AddModule(new ImageGalleryModule());

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
            //var scene = assetsService.Load<Scenes.StorageScene>(EvergineContent.Scenes.Empty_wescene);
            var scene = assetsService.Load<Scenes.EmptyScene>(EvergineContent.Scenes.Empty_wescene);
            //var scene = assetsService.Load<Scenes.WindowScene>(EvergineContent.Scenes.Windows_wescene);

            ScreenContext screenContext = new ScreenContext(scene);
            screenContextManager.To(screenContext);
        }
    }
}
