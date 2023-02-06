using Evergine.Framework;
using Evergine.Platform;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Networking;

namespace XrvSamples.Scenes
{
    public class EmptyScene : BaseScene
    {
        private XrvService xrv;

        protected override void OnPostCreateXRScene()
        {
            base.OnPostCreateXRScene();

            xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);

            // Networking
            ////var configuration = new NetworkConfigurationBuilder()
            ////    .ForApplication(nameof(XrvSamples))
            ////    .UsePort(DeviceInfo.PlatformType == Evergine.Common.PlatformType.UWP ? 12345 : 12344)
            ////    .Build();
            ////xrv.Networking.Configuration = configuration;
            ////xrv.Networking.OverrideScanningPort = 12345; // UWP client should be always the host
            ////xrv.Networking.NetworkingAvailable = true;
            ////xrv.Services.QrScanningFlow.ExpectedCodes = new[] { "This is XRV!" };
        }       
    }
}