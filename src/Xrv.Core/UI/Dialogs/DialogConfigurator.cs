using Evergine.Components.Fonts;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.UI.Dialogs
{
    public class DialogConfigurator : BaseWindowConfigurator
    {
        private string text;
        protected Text3DMesh textMesh;

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

        private void UpdateText() => this.textMesh.Text = this.text;

        protected override void OnActivated()
        {
            base.OnActivated();
            this.textMesh = this.Owner.FindComponentInChildren<Text3DMesh>(tag: "PART_base_dialog_text");
            this.UpdateText();
        }
    }
}
