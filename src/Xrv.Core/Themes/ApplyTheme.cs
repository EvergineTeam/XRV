// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.UI.Tabs;

namespace Xrv.Core.Themes
{
    /// <summary>
    /// Applies current theme for owner entity hierarchy (in depth). Elements that will be considered for theming are:
    /// - Text3DMesh.
    /// - Buttons.
    /// - Tab controls.
    /// </summary>
    public class ApplyTheme : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private IEnumerable<Text3DMesh> texts = null;
        private IEnumerable<TabControl> tabs = null;

        /// <summary>
        /// Applies theme colors in depth.
        /// </summary>
        public void Apply()
        {
            if (this.xrvService?.ThemesSystem?.CurrentTheme is Theme theme)
            {
                this.ApplyThemeOnTexts(theme);
                this.ApplyThemeOnTabControls(theme);
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.texts = this.Owner.FindComponentsInChildren<Text3DMesh>();
                this.tabs = this.Owner.FindComponentsInChildren<TabControl>();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.Apply();
        }

        private void ApplyThemeOnTexts(Theme theme)
        {
            if (this.texts == null)
            {
                return;
            }

            foreach (var text in this.texts)
            {
                var configurators = text.Owner.FindComponentsInParents<StandardButtonConfigurator>(isExactType: false);
                if (configurators.Any())
                {
                    foreach (var configurator in configurators)
                    {
                        configurator.PrimaryColor = theme[ThemeColor.PrimaryColor3];
                    }
                }
                else
                {
                    text.Color = theme[ThemeColor.PrimaryColor3];
                }
            }
        }

        private void ApplyThemeOnTabControls(Theme theme)
        {
            if (this.tabs == null)
            {
                return;
            }

            foreach (var tab in this.tabs)
            {
                tab.ActiveItemTextColor = theme[ThemeColor.PrimaryColor3];
                tab.InactiveItemTextColor = theme[ThemeColor.SecondaryColor1];
            }
        }
    }
}
