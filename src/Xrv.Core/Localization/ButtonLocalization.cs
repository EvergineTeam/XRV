// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;

namespace Xrv.Core.Localization
{
    /// <summary>
    /// Controls localization for standard buttons.
    /// </summary>
    public class ButtonLocalization : BaseLocalization
    {
        [BindComponent]
        private StandardButtonConfigurator configurator = null;

        /// <inheritdoc/>
        protected override void SetText(string text) =>
            this.configurator.Text = text;
    }
}
