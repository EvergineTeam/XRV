using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
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

        public Entity CreateInstance(HandMenuButtonDescription description) =>
            description.IsToggle ? CreateToggleButton(description) : CreateStandardButton(description);

        private Entity CreateStandardButton(HandMenuButtonDescription description)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new StandardButtonConfigurator
            {
                Text = description.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
            });
            this.AssociateActivationPublishers(description, button);
            Workarounds.MrtkForceButtonNullPlate(button);

            return button;
        }

        private Entity CreateToggleButton(HandMenuButtonDescription description)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.AddComponent(new ToggleButton());
            button.AddComponent(new ToggleButtonConfigurator
            {
                TargetState = ToggleState.Off,
                Text = description.TextOff,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOff),
            });
            button.AddComponent(new ToggleButtonConfigurator
            {
                TargetState = ToggleState.On,
                Text = description.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
            });

            this.AssociateActivationPublishers(description, button);
            Workarounds.MrtkForceButtonNullPlate(button);

            return button;
        }

        private void AssociateActivationPublishers(HandMenuButtonDescription description, Entity button)
        {
            var associatedModule = this.xrvService.GetModuleForHandButton(description);
            if (associatedModule != null)
            {
                button.AddComponent(new ActivateModuleOnButtonPress
                {
                    Module = associatedModule,
                });
            }
            else
            {
                button.AddComponent(new HandMenuButtonPress
                {
                    Description = description,
                });
            }
        }

        private Prefab GetButtonPrefab() =>
            this.assetsService.Load<Prefab>(DefaultResourceIDs.Mrtk.Prefabs.PressableButtonPlated);
    }
}
