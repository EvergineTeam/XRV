// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;

namespace Xrv.Core.UI.Dialogs
{
    public class AlertDialog : Dialog
    {
        public const string AcceptKey = nameof(AcceptKey);

        [IgnoreEvergine]
        public DialogOption AcceptOption { get; } = new DialogOption(AcceptKey);

        protected override void InstantiateOptions()
        {
            this.Clear();

            var acceptButton = this.CreateButtonInstance(this.AcceptOption);
            this.AddOption(acceptButton.FindComponent<PressableButton>(), this.AcceptOption);
            this.singleButtonHolder?.AddChild(acceptButton);
        }
    }
}
