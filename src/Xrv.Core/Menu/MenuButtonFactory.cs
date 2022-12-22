// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.Input.Handlers;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Xrv.Core.Extensions;
using Xrv.Core.Modules;
using Xrv.Core.Networking.ControlRequest;
using Xrv.Core.UI.Buttons;
using Xrv.Core.VoiceCommands;

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
                AllowBackPlateNullMaterial = true,
            });

            if (!string.IsNullOrEmpty(description.VoiceCommandOn))
            {
                button.AddComponent(new PressableButtonSpeechHandler
                {
                    SpeechHandlerFireCondition = SpeechHandlerFireCondition.Global,
                    SpeechKeywords = new[] { description.VoiceCommandOn },
                });
            }

            this.AssociateActivationPublishers(description, button);
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

            if (!string.IsNullOrEmpty(description.VoiceCommandOn)
                || !string.IsNullOrEmpty(description.VoiceCommandOff))
            {
                button.AddComponent(new ToggleButtonSpeechHandler
                {
                    SpeechHandlerFireCondition = SpeechHandlerFireCondition.Global,
                    OnKeywords = string.IsNullOrEmpty(description.VoiceCommandOn) ? null : new[] { description.VoiceCommandOn },
                    OffKeywords = string.IsNullOrEmpty(description.VoiceCommandOff) ? null : new[] { description.VoiceCommandOff },
                });
            }

            button.AddComponent(new ToggleButtonConfigurator
            {
                TargetState = ToggleState.On,
                Text = description.TextOn,
                Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
                AllowBackPlateNullMaterial = true,
            });

            this.AssociateActivationPublishers(description, button);
            var lookAndFeel = XrvPressableButtonLookAndFeel.ApplyTo(button);
            lookAndFeel.TextPositionOffset = -0.002f;

            return button;
        }

        private void AssociateActivationPublishers(MenuButtonDescription description, Entity button)
        {
            button.AddComponent(new VisuallyEnabledController());

            var associatedModule = this.xrvService.GetModuleForHandButton(description);
            if (associatedModule != null)
            {
                button
                    .AddComponent(new ActivateModuleOnButtonPress
                    {
                        Module = associatedModule,
                    })
                    .AddComponent(new ButtonEnabledStateByControlStatus());
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
            this.assetsService.Load<Prefab>(MRTKResourceIDs.Prefabs.PressableButtonPlated);
    }
}
