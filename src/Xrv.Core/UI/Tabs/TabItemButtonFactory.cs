using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;

namespace Xrv.Core.UI.Tabs
{
    internal class TabItemButtonFactory
    {
        private readonly AssetsService assetsService;

        public TabItemButtonFactory(AssetsService assetsService)
        {
            this.assetsService = assetsService;
        }

        public static TabItemButtonFactory Instance { get; internal set; }

        public Entity CreateInstance(TabItem item)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new StandardButtonConfigurator
            {
                Text = item.Text,
                Icon = null,
            });
            button.AddComponent(new TabItemAssociation
            {
                Item = item,
                SelectedTextColor = Color.White,
                UnselectedTextColor = Color.FromHex("#70F2F8"),
            });

            Workarounds.MrtkForceButtonNullPlate(button);

            return button;
        }

        private Prefab GetButtonPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Prefabs.TextButton);
    }
}
