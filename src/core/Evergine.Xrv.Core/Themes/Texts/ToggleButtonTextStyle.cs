// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Evergine.Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Text style component for toggle buttons.
    /// </summary>
    [AllowMultipleInstances]
    public class ToggleButtonTextStyle : BaseTextStyleComponent
    {
        /// <summary>
        /// Gets or sets target state associated to toggle button that this component
        /// will provide localized text to.
        /// </summary>
        public ToggleState TargetState { get; set; }

        /// <inheritdoc/>
        protected override void ApplyTextStyle(TextStyle style)
        {
            var configurator = this.FindConfiguratorForThisState();
            if (configurator != null)
            {
                this.ApplyStyleToConfigurator(configurator, style);
            }
        }

        private void ApplyStyleToConfigurator(ToggleButtonConfigurator configurator, TextStyle style)
        {
            var font = this.GetPreferredFont(style);
            if (font != null)
            {
                configurator.Font = font;
            }

            configurator.TextScale = style.TextScale;

            var color = this.GetPreferredColor(style);
            if (color.HasValue)
            {
                configurator.PrimaryColor = color.Value;
            }
        }

        private ToggleButtonConfigurator FindConfiguratorForThisState()
        {
            var configurator = this.Owner
                .FindComponentsInChildren<ToggleButtonConfigurator>(isExactType: false)
                .FirstOrDefault(configurator => configurator.TargetState == this.TargetState);

            return configurator;
        }
    }
}
