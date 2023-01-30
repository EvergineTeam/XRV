// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using System.Linq;
using Evergine.Xrv.Core.Localization;

namespace Evergine.Xrv.Core.UI.Tabs
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
                Icon = null,
                AllowBackPlateNullMaterial = true,
            });
            button.AddComponent(new ButtonLocalization
            {
                LocalizationFunc = item.Name,
            });

            var cage = button.FindChildrenByTag("PART_text_button_cage", true).First();
            cage.IsEnabled = false;

            return button;
        }

        private Prefab GetButtonPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
    }
}
