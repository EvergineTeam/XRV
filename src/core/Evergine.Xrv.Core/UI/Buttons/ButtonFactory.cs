// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.Input.Handlers;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Themes.Texts;
using Evergine.Xrv.Core.VoiceCommands;
using System;
using System.Linq;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Create menu button factory.
    /// </summary>
    public class ButtonFactory
    {
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
        public Entity CreateInstance(ButtonDescription description) =>
            this.CreateInstance(description, description.IsToggle ? CoreResourcesIDs.Prefabs.baseToggleButton_weprefab : CoreResourcesIDs.Prefabs.baseButton_weprefab);

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

            return button;
        }

        private Entity CreateToggleButton(ButtonDescription description, Guid prefabId)
        {
            var prefab = this.assetsService.Load<Prefab>(prefabId);
            var button = prefab.Instantiate();
            button.Flags = HideFlags.DontSave | HideFlags.DontShow;

            var configurators = button.FindComponentsInChildren<ToggleButtonConfigurator>(isExactType: false);
            var offConfiguration = configurators.FirstOrDefault(c => c.TargetState == ToggleState.Off);
            if (offConfiguration != null)
            {
                offConfiguration.Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOff);
            }

            button
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

            var onConfiguration = configurators.FirstOrDefault(c => c.TargetState == ToggleState.On);
            if (onConfiguration != null)
            {
                onConfiguration.Icon = this.assetsService.LoadIfNotDefaultId<Material>(description.IconOn);
            }

            button
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

            return button;
        }
    }
}
