using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Themes;

namespace XrvSamples.Scenes
{
    internal class CustomThemeScene : BaseScene
    {
        private XrvService xrv;

        protected override void OnPostCreateXRScene()
        {
            base.OnPostCreateXRScene();

            xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);

            var theme = xrv.ThemesSystem.CurrentTheme;
            theme.PrimaryColor1 = Color.Red;
            theme.PrimaryColor2 = Color.Green;
            theme.PrimaryColor3 = Color.Yellow;
            theme.SecondaryColor1 = Color.DarkGreen;
            theme.SecondaryColor2 = Color.Purple;
            theme.SecondaryColor3 = Color.Pink;
            theme.SecondaryColor4 = Color.DarkGoldenrod;
            theme.SecondaryColor5 = Color.Orange;
        }
    }
}
