// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.Input.Handlers;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Xrv.Core.VoiceCommands
{
    public class ToggleButtonSpeechHandler : SpeechHandler
    {
        [BindComponent(source: BindComponentSource.Children)]
        private ToggleButton button = null;

        [IgnoreEvergine]
        public new string[] SpeechKeywords
        {
            get => base.SpeechKeywords;
            protected set => base.SpeechKeywords = value;
        }

        public string[] OffKeywords { get; set; }

        public string[] OnKeywords { get; set; }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.SpeechKeywords = (this.OnKeywords ?? Enumerable.Empty<string>())
                    .Union(this.OffKeywords ?? Enumerable.Empty<string>())
                    .ToArray();
            }

            return attached;
        }

        protected override void InternalOnSpeechKeywordRecognized(string keyword)
        {
            base.InternalOnSpeechKeywordRecognized(keyword);

            if (this.OffKeywords?.Contains(keyword) == true && !this.button.IsOn)
            {
                Workarounds.ChangeToggleButtonState(this.button.Owner, true);
            }
            else if (this.OnKeywords?.Contains(keyword) == true && this.button.IsOn)
            {
                Workarounds.ChangeToggleButtonState(this.button.Owner, false);
            }
        }
    }
}
