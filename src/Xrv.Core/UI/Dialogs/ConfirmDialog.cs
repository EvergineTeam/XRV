using Evergine.Common.Attributes;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;

namespace Xrv.Core.UI.Dialogs
{
    public class ConfirmDialog : Dialog
    {
        public const string CancelKey = nameof(CancelKey);
        public const string AcceptKey = nameof(AcceptKey);

        [IgnoreEvergine]
        public DialogOption CancelOption { get; } = new DialogOption(CancelKey);

        [IgnoreEvergine]
        public DialogOption AcceptOption { get; } = new DialogOption(AcceptKey);

        protected override void InstantiateOptions()
        {
            this.Clear();

            var cancelButton = this.CreateButtonInstance(this.CancelOption);
            this.AddOption(cancelButton.FindComponent<PressableButton>(), this.CancelOption);
            this.cancelHolder.AddChild(cancelButton);

            var acceptButton = this.CreateButtonInstance(this.AcceptOption);
            this.AddOption(acceptButton.FindComponent<PressableButton>(), this.AcceptOption);
            this.acceptHolder.AddChild(acceptButton);
        }
    }
}
