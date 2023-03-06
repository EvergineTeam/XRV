// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;

namespace Evergine.Xrv.Core.Help
{
    /// <summary>
    /// Controls about panel presentation.
    /// </summary>
    public class AboutController : Component
    {
        private const string DefaultContents = "Copyright @ Plain Concepts. S.L.U . All rights reserved /n/n Powered by Evergine.";

        [BindService]
        private XrvService xrv = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_About_Version")]
        private Text3DMesh versionText = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_About_Text")]
        private Text3DMesh contentText = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.LoadVersionText();
                this.LoadContentText();
            }

            return attached;
        }

        private void LoadVersionText()
        {
            var versionProvider = new ApplicationInfoProvider();
            this.versionText.Text = versionProvider.GetVersion();
        }

        private void LoadContentText() =>
            this.contentText.Text = this.xrv?.HelpSystem?.AboutContents?.Invoke() ?? DefaultContents;
    }
}
