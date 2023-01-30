// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using System;

namespace Evergine.Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Extended configuration for dialog options, to include localization
    /// capabilities.
    /// </summary>
    public class OptionButtonConfigurator : StandardButtonConfigurator
    {
        private Func<string> localizedText;

        /// <summary>
        /// Gets or sets a function to retrieve localization for option text.
        /// </summary>
        [IgnoreEvergine]
        public Func<string> LocalizedText
        {
            get => this.localizedText;

            set
            {
                if (this.localizedText != value)
                {
                    this.localizedText = value;
                    this.Text = this.localizedText?.Invoke();
                }
            }
        }
    }
}
