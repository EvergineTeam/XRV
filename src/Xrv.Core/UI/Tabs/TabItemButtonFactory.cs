using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using System.Linq;

namespace Xrv.Core.UI.Tabs
{
    internal class TabItemButtonFactory
    {
        private readonly AssetsService assetsService;

        public TabItemButtonFactory(AssetsService assetsService)
        {
            this.assetsService = assetsService;
        }

        public Entity CreateInstance(TabItem item)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new StandardButtonConfigurator
            {
                Text = item.Text,
                Icon = null,
            });

            var cage = button.FindChildrenByTag("PART_text_button_cage", true).First();
            cage.IsEnabled = false;

            Workarounds.MrtkForceButtonNullPlate(button);

            return button;
        }

        private Prefab GetButtonPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Prefabs.TextButton);
    }
}
