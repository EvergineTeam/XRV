using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;
using Xrv.Core.Extensions;
using Xrv.Core.Modules;

namespace Xrv.Core.Menu
{
    internal class HandMenuButtonFactory
    {
        private readonly XrvService xrvService;
        private readonly AssetsService assetsService;

        public HandMenuButtonFactory(XrvService xrvService, AssetsService assetsService)
        {
            this.xrvService = xrvService;
            this.assetsService = assetsService;
        }

        public Entity CreateInstance(HandMenuButtonDefinition definition) =>
            definition.IsToggle ? CreateToggleButton(definition) : CreateStandardButton(definition);

        private Entity CreateStandardButton(HandMenuButtonDefinition definition)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new StandardButtonConfigurator
            {
                Text = definition.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(definition.IconOn),
            });
            this.SetModuleAssociation(definition, button);
            FixUpMrtkIssue(button);

            return button;
        }

        private Entity CreateToggleButton(HandMenuButtonDefinition definition)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new ToggleButton());
            button.AddComponent(new ToggleButtonConfigurator
            {
                TargetState = ToggleState.Off,
                Text = definition.TextOff,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(definition.IconOff),
            });
            button.AddComponent(new ToggleButtonConfigurator
            {
                TargetState = ToggleState.On,
                Text = definition.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(definition.IconOn),
            });

            this.SetModuleAssociation(definition, button);
            FixUpMrtkIssue(button);

            return button;
        }

        private void SetModuleAssociation(HandMenuButtonDefinition definition, Entity button)
        {
            var associatedModule = this.xrvService.GetModuleForHandButton(definition);
            if (associatedModule != null)
            {
                button.AddComponent(new ActivateModuleOnButtonRelease
                {
                    Module = associatedModule,
                });
            }
        }

        private Prefab GetButtonPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Mrtk.Prefabs.PressableButtonPlated);

        private static void FixUpMrtkIssue(Entity button)
        {
            // Workaround MTRK ignores null material on StandardButtonConfigurator
            button.FindChildrenByTag("PART_Plate", true).First().FindComponent<MaterialComponent>().Material = null;
        }
    }
}
