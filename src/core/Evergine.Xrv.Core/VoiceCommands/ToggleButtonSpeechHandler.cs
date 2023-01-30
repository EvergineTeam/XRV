// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.Input.Handlers;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Evergine.Xrv.Core.VoiceCommands
{
    /// <summary>
    /// Speech handler for toggle buttons.
    /// </summary>
    public class ToggleButtonSpeechHandler : SpeechHandler
    {
        [BindComponent(source: BindComponentSource.Children)]
        private ToggleButton button = null;

        /// <summary>
        ///  Gets the words that will make this speech handler to trigger.
        /// </summary>
        [IgnoreEvergine]
        public new string[] SpeechKeywords
        {
            get => base.SpeechKeywords;
            private set => base.SpeechKeywords = value;
        }

        /// <summary>
        ///  Gets or sets the words that will make this speech handler to trigger for Off state.
        /// </summary>
        public string[] OffKeywords { get; set; }

        /// <summary>
        ///  Gets or sets the words that will make this speech handler to trigger for On state.
        /// </summary>
        public string[] OnKeywords { get; set; }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
