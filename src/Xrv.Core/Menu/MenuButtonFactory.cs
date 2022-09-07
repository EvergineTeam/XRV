// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Xrv.Core.Extensions;
using Xrv.Core.Modules;
using Xrv.Core.UI.Buttons;

namespace Xrv.Core.Menu
{
    /// <summary>
    /// Create menu button factory.
    /// </summary>
    public class MenuButtonFactory
    {
        private readonly XrvService xrvService;
        private readonly AssetsService assetsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuButtonFactory"/> class.
        /// </summary>
        /// <param name="xrvService">XRV service.</param>
        /// <param name="assetsService">Assets Service.</param>
        public MenuButtonFactory(XrvService xrvService, AssetsService assetsService)
        {
            this.xrvService = xrvService;
            this.assetsService = assetsService;
        }

        /// <summary>
        /// Creates an instance of a button from its description.
        /// </summary>
        /// <param name="description">Button description.</param>
        /// <returns>Button entity.</returns>
        public Entity CreateInstance(MenuButtonDescription description) =>
            description.IsToggle ? this.CreateToggleButton(description) : this.CreateStandardButton(description);

        private Entity CreateStandardButton(MenuButtonDescription description)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.Flags = HideFlags.DontSave | HideFlags.DontShow;
            button.AddComponent(new StandardButtonConfigurator
            {
                Text = description.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
            });
            this.AssociateActivationPublishers(description, button);
            Workarounds.MrtkForceButtonNullPlate(button);
            var lookAndFeel = XrvPressableButtonLookAndFeel.ApplyTo(button);
            lookAndFeel.TextPositionOffset = -0.002f;

            return button;
        }

        private Entity CreateToggleButton(MenuButtonDescription description)
        {
            var prefab = this.GetButtonPrefab();
            var button = prefab.Instantiate();
            button.Flags = HideFlags.DontSave | HideFlags.DontShow;
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
            var lookAndFeel = XrvPressableButtonLookAndFeel.ApplyTo(button);
            lookAndFeel.TextPositionOffset = -0.002f;

            return button;
        }

        private void AssociateActivationPublishers(MenuButtonDescription description, Entity button)
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
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Mrtk.Prefabs.PressableButtonPlated);
    }
}
