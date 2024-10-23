using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Networking;
using Evergine.Xrv.Core.Networking.Participants;

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
            // Note: to use more than one client in desktop, ensure
            // they use different ports. This will affect scan reachability.
            ////var configuration = new NetworkConfigurationBuilder()
            ////    .ForApplication(nameof(XrvSamples))
            ////    .UsePort(12345)
            ////    .SetQrCodeForSession("This is XRV!")
            ////    .Build();
            ////xrv.Networking.Configuration = configuration;
            ////////xrv.Networking.OverrideScanningPort = 12345;
            ////xrv.Networking.NetworkingAvailable = true;
            
            xrv.Services.Passthrough.EnablePassthrough = true;
        }
    }
}