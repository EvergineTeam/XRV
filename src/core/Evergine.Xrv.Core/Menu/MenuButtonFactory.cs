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
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Networking.ControlRequest;
using Evergine.Xrv.Core.Themes.Texts;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.VoiceCommands;

namespace Evergine.Xrv.Core.Menu
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
            button
                .AddComponent(new StandardButtonConfigurator
                {
                    Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
                    AllowBackPlateNullMaterial = true,
                })
                .AddComponent(new ButtonLocalization
                {
                    LocalizationFunc = description.TextOn,
                })
                .AddComponent(new ButtonTextStyle
                {
                    TextStyleKey = DefaultTextStyles.XrvPrimary2Size3,
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
            button
                .AddComponent(new ToggleButton())
                .AddComponent(new ToggleButtonConfigurator
                {
                    TargetState = ToggleState.Off,
                    Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOff),
                })
                .AddComponent(new ToggleButtonLocalization
                {
                    TargetState = ToggleState.Off,
                    LocalizationFunc = description.TextOff,
                })
                .AddComponent(new ToggleButtonTextStyle
                {
                    TargetState = ToggleState.Off,
                    TextStyleKey = DefaultTextStyles.XrvPrimary2Size3,
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

            button
                .AddComponent(new ToggleButtonConfigurator
                {
                    TargetState = ToggleState.On,
                    Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn),
                    AllowBackPlateNullMaterial = true,
                })
                .AddComponent(new ToggleButtonLocalization
                {
                    TargetState = ToggleState.On,
                    LocalizationFunc = description.TextOn,
                })
                .AddComponent(new ToggleButtonTextStyle
                {
                    TargetState = ToggleState.On,
                    TextStyleKey = DefaultTextStyles.XrvPrimary2Size3,
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
