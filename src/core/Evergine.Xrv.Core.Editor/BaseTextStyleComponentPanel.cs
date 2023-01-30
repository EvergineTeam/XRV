using Evergine.Editor.Extension;
using Evergine.Editor.Extension.Attributes;
using Evergine.Framework.Graphics;
using System.Linq;
using Evergine.Xrv.Core.Themes.Texts;

namespace Evergine.Xrv.Core.Editor
{
    [CustomPanelEditor(typeof(BaseTextStyleComponent))]
    public class BaseTextStyleComponentPanel : PanelEditor
    {
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
        }
    }
}
