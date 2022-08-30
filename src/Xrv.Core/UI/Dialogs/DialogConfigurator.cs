// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Configuration for dialogs.
    /// </summary>
    public class DialogConfigurator : BaseWindowConfigurator
    {
        private string text;
        private Text3DMesh textMesh;

        /// <summary>
        /// Gets or sets dialog text.
        /// </summary>
        public string Text
        {
            get => this.text;
            set
            {
                this.text = value;
                if (this.IsActivated)
                {
                    this.UpdateText();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.textMesh = this.Owner.FindComponentInChildren<Text3DMesh>(tag: "PART_base_dialog_text");
            this.UpdateText();
        }

        private void UpdateText() => this.textMesh.Text = this.text;
    }
}
