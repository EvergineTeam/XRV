// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;

namespace Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Alert dialog.
    /// </summary>
    public class AlertDialog : Dialog
    {
        /// <summary>
        /// Key for accept button.
        /// </summary>
        public const string AcceptKey = nameof(AcceptKey);

        /// <summary>
        /// Gets accept option.
        /// </summary>
        [IgnoreEvergine]
        public DialogOption AcceptOption { get; } = new DialogOption(AcceptKey);

        /// <inheritdoc/>
        protected override void InstantiateOptions()
        {
            var acceptButton = this.CreateButtonInstance(this.AcceptOption);
            this.AddOption(acceptButton.FindComponent<PressableButton>(), this.AcceptOption);
            this.singleButtonHolder?.AddChild(acceptButton);
        }
    }
}
