// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;

namespace Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Confirmation dialog.
    /// </summary>
    public class ConfirmationDialog : Dialog
    {
        /// <summary>
        /// Key for cancel button.
        /// </summary>
        public const string CancelKey = nameof(CancelKey);

        /// <summary>
        /// Key for accept button.
        /// </summary>
        public const string AcceptKey = nameof(AcceptKey);

        /// <summary>
        /// Gets cancel option.
        /// </summary>
        [IgnoreEvergine]
        public DialogOption CancelOption { get; } = new DialogOption(CancelKey);

        /// <summary>
        /// Gets accept option.
        /// </summary>
        [IgnoreEvergine]
        public DialogOption AcceptOption { get; } = new DialogOption(AcceptKey);

        /// <inheritdoc/>
        protected override void InstantiateOptions()
        {
            var cancelButton = this.CreateButtonInstance(this.CancelOption);
            this.AddOption(cancelButton.FindComponent<PressableButton>(), this.CancelOption);
            this.cancelHolder.AddChild(cancelButton);

            var acceptButton = this.CreateButtonInstance(this.AcceptOption);
            this.AddOption(acceptButton.FindComponent<PressableButton>(), this.AcceptOption);
            this.acceptHolder.AddChild(acceptButton);
        }
    }
}
