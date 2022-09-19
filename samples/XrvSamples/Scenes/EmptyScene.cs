using Evergine.Common.Graphics;
using Evergine.Framework;
using Xrv.Core;

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
        }       
    }
}