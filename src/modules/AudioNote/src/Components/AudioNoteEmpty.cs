using Evergine.Components.WorkActions;
using Evergine.Framework;
using System;
using Xrv.AudioNote.Messages;
using Xrv.Core.UI.Dialogs;

namespace Xrv.AudioNote
{
    public class AudioNoteEmpty : AudioNoteBase
    {
        private bool isRecording;
        protected override void ActionButton_ButtonReleased(object sender, EventArgs e)
        {
            base.ActionButton_ButtonReleased(sender, e);
            this.isRecording = !this.isRecording;
            if (this.isRecording)
            {
                if (string.IsNullOrEmpty(this.Data.Path))
                {
                    this.BeginRecordAudionote();
                }
                else
                {
                    var alert = this.xrvService.WindowSystem.ShowConfirmDialog("Override this audio?", "This action can’t be undone.", "No", "Yes");
                    alert.Open();
                    alert.Closed += Alert_Closed;
                }
            }
            else
            {
                this.SaveContent();
            }
        }

        private void SaveContent()
        {
            // TODO stop record
            // TODO do save content here
            if (!string.IsNullOrEmpty(this.Data.Path))
            {
                // TODO remove previous record
            }

            this.Data.Path = Guid.NewGuid().ToString();
            // Update anchor that this is no longer an Empty Anchor
            this.xrvService.PubSub.Publish(new AudioNoteUpdateMessage() { Data = this.Data });
        }

        private void BeginRecordAudionote()
        {
            // TODO begin record            
        }

        private void Alert_Closed(object sender, EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Alert_Closed;

                var isAcceted = dialog.Result == ConfirmDialog.AcceptKey;

                if (isAcceted)
                {
                    this.BeginRecordAudionote();
                }
                else
                {
                    this.ShowContent(false);
                }
            }
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            if (Application.Current.IsEditor) return;

            if (this.isRecording)
            {
                this.SaveContent();
            }

            if (!string.IsNullOrEmpty(this.Data.Path))
            {
                // If this note has a content, is no longer EmptyNote
                WorkActionFactory.CreateDelayWorkAction(this.Owner.Scene, TimeSpan.FromSeconds(0.5f))
                    .ContinueWithAction(() => this.xrvService.PubSub.Publish(new AudioNoteWindowDeleteMessage() { Data = this.Data }))
                    .Run();
            }
        }
    }
}
