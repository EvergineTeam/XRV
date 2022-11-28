using Evergine.Common.Graphics;
using Evergine.Framework;
using Xrv.Core;
using Xrv.Core.Themes;

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
            theme[ThemeColor.PrimaryColor1] = Color.Red;
            theme[ThemeColor.PrimaryColor2] = Color.Green;
            theme[ThemeColor.PrimaryColor3] = Color.Yellow;
            theme[ThemeColor.SecondaryColor1] = Color.DarkGreen;
            theme[ThemeColor.SecondaryColor2] = Color.Purple;
            theme[ThemeColor.SecondaryColor3] = Color.Pink;
            theme[ThemeColor.SecondaryColor4] = Color.DarkGoldenrod;
            theme[ThemeColor.SecondaryColor5] = Color.Orange;
        }
    }
}
