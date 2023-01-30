// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Components.Fonts;
using System;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Configuration for dialogs.
    /// </summary>
    public class DialogConfigurator : BaseWindowConfigurator
    {
        private Func<string> localizedText;
        private Text3DMesh textMesh;
        private Text3dLocalization textLocalization;

        /// <summary>
        /// Gets or sets localized dialog message text.
        /// </summary>
        [IgnoreEvergine]
        public Func<string> LocalizedText
        {
            get => this.textLocalization != null ? this.textLocalization.LocalizationFunc : this.localizedText;
            set
            {
                if (this.textLocalization != null)
                {
                    this.textLocalization.LocalizationFunc = value;
                }
                else
                {
                    this.localizedText = value;
                    if (this.IsActivated)
                    {
                        this.UpdateText();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.textMesh = this.Owner.FindComponentInChildren<Text3DMesh>(tag: "PART_base_dialog_text");
            this.textLocalization = this.Owner.FindComponentInChildren<Text3dLocalization>(tag: "PART_base_dialog_text");
            this.UpdateText();
        }

        private void UpdateText() => this.textMesh.Text = this.localizedText.Invoke();
    }
}
