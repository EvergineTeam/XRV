using Evergine.Editor.Extension;
using Evergine.Editor.Extension.Attributes;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.Themes.Texts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.Editor
{
    [CustomPanelEditor(typeof(BaseTextStyleComponent))]
    public class BaseTextStyleComponentPanel : PanelEditor
    {
        private static List<string> themedColors;

        static BaseTextStyleComponentPanel()
        {
            themedColors = Enum
                .GetValues(typeof(ThemeColor))
                .Cast<ThemeColor>()
                .Select(color => color.ToString())
                .OrderBy(color => color)
                .ToList();

            themedColors.Insert(0, string.Empty);
        }

        public new BaseTextStyleComponent Instance => (BaseTextStyleComponent)base.Instance;

        public override void GenerateUI()
        {
            base.GenerateUI();

            this.propertyPanelContainer.AddSelector(
                nameof(BaseTextStyleComponent.TextStyleKey),
                nameof(BaseTextStyleComponent.TextStyleKey),
                TextStylesRegister.TextStyles.Keys.OrderBy(k => k),
                () => this.Instance.TextStyleKey,
                x => this.Instance.TextStyleKey = x);

            this.propertyPanelContainer.AddSelector(
                nameof(BaseTextStyleComponent.ExplicitThemeColor),
                nameof(BaseTextStyleComponent.ExplicitThemeColor),
                themedColors,
                () => this.Instance.ExplicitThemeColor.ToString(),
                color => this.Instance.ExplicitThemeColor = TryParseThemedColor(color));
        }

        private static ThemeColor TryParseThemedColor(string color)
        {
            ThemeColor themeColor;
            if (!Enum.TryParse(color, out themeColor))
            {
                return default;
            }

            return themeColor;
        }
    }
}
