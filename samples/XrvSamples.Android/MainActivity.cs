using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Diagnostics;
using Evergine.Common.Graphics;
using Evergine.Framework.Services;
using Evergine.Vulkan;
using Display = Evergine.Framework.Graphics.Display;
using Surface = Evergine.Common.Graphics.Surface;

namespace XrvSamples.Android
{
    [Activity(Label = "@string/app_name",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : global::Android.App.Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set fullscreen surface
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            this.Window.AddFlags(WindowManagerFlags.Fullscreen);

            // Set Main layout
            this.SetContentView(Resource.Layout.Main);

            // Create app
            MyApplication application = new MyApplication();

            // Create Services
            WindowsSystem windowsSystem = new global::Evergine.AndroidView.AndroidWindowsSystem(this);
            application.Container.RegisterInstance(windowsSystem);
            var surface = windowsSystem.CreateSurface(0, 0) as global::Evergine.AndroidView.AndroidSurface;

            var view = this.FindViewById<RelativeLayout>(Resource.Id.evergineContainer);
            view.AddView(surface.NativeSurface);

            // Creates XAudio device
            var xaudio = new global::Evergine.OpenAL.ALAudioDevice();
            application.Container.RegisterInstance(xaudio);

            Stopwatch clockTimer = Stopwatch.StartNew();
            windowsSystem.Run(
            () =>
            {
                ConfigureGraphicsContext(application, surface);
                application.Initialize();
            },
            () =>
            {
                var gameTime = clockTimer.Elapsed;
                clockTimer.Restart();

                application.UpdateFrame(gameTime);
                application.DrawFrame(gameTime);
            });

        }

        private void ConfigureGraphicsContext(MyApplication application, Surface surface)
        {
            GraphicsContext graphicsContext = new VKGraphicsContext();
            graphicsContext.CreateDevice();
            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                SurfaceInfo = surface.SurfaceInfo,
                Width = surface.Width,
                Height = surface.Height,
                ColorTargetFormat = PixelFormat.R8G8B8A8_UNorm,
                ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
                DepthStencilTargetFormat = PixelFormat.D24_UNorm_S8_UInt,
                DepthStencilTargetFlags = TextureFlags.DepthStencil,
                SampleCount = TextureSampleCount.None,
                IsWindowed = true,
                RefreshRate = 60
            };
            var swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
            swapChain.VerticalSync = true;

            var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
            var firstDisplay = new Display(surface, swapChain);
            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);

            application.Container.RegisterInstance(graphicsContext);
        }
    }
}

