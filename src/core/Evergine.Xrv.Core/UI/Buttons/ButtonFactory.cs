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
using Evergine.Xrv.Core.Themes.Texts;
using Evergine.Xrv.Core.VoiceCommands;
using System;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Create menu button factory.
    /// </summary>
    public class ButtonFactory
    {
        private const float TextPositionHover = -0.002f;
        private readonly AssetsService assetsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonFactory"/> class.
        /// </summary>
        /// <param name="assetsService">Assets Service.</param>
        public ButtonFactory(AssetsService assetsService)
        {
            this.assetsService = assetsService;
        }

        /// <summary>
        /// Creates an instance of a button from its description.
        /// </summary>
        /// <param name="description">Button description.</param>
        /// <returns>Button entity.</returns>
        public Entity CreateInstance(ButtonDescription description) => this.CreateInstance(description, MRTKResourceIDs.Prefabs.PressableButtonPlated);

        /// <summary>
        /// Creates an instance of a button from its description.
        /// </summary>
        /// <param name="description">Button description.</param>
        /// <param name="prefabId">Prefab identifier to create button instance.</param>
        /// <returns>Button entity.</returns>
        public Entity CreateInstance(ButtonDescription description, Guid prefabId) =>
            description.IsToggle ? this.CreateToggleButton(description, prefabId) : this.CreateStandardButton(description, prefabId);

        private Entity CreateStandardButton(ButtonDescription description, Guid prefabId)
        {
            var prefab = this.assetsService.Load<Prefab>(prefabId);
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
                })
                .AddComponent(new XrvPressableButtonLookAndFeel
                {
                    TextPositionOffset = TextPositionHover,
                });

            if (!string.IsNullOrEmpty(description.VoiceCommandOn))
            {
                button.AddComponent(new PressableButtonSpeechHandler
                {
                    SpeechHandlerFireCondition = SpeechHandlerFireCondition.Global,
                    SpeechKeywords = new[] { description.VoiceCommandOn },
                });
            }

            this.AddCommonComponents(button);

            return button;
        }

        private Entity CreateToggleButton(ButtonDescription description, Guid prefabId)
        {
            var prefab = this.assetsService.Load<Prefab>(prefabId);
            var button = prefab.Instantiate();
            button.Flags = HideFlags.DontSave | HideFlags.DontShow;
            button
                .AddComponent(new ToggleButton())
                .AddComponent(new ToggleButtonConfigurator
                {
                    TargetState = ToggleState.Off,
                    Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOff),
                    AllowBackPlateNullMaterial = true,
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
                })
                .AddComponent(new XrvPressableButtonLookAndFeel
                {
                    TextPositionOffset = TextPositionHover,
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

            this.AddCommonComponents(button);

            return button;
        }

        private void AddCommonComponents(Entity button)
        {
            button.AddComponent(new VisuallyEnabledController());
        }
    }
}
