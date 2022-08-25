using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.Core;

namespace Xrv.AudioNote
{
    public class AudioNoteBase : Component
    {
        [BindService]
        protected XrvService xrvService;

        [BindEntity(tag: "Content", isRequired: false)]
        protected Entity content;

        [BindComponent(source: BindComponentSource.Children, tag: "ContentAction", isRequired: false)]
        protected PressableButton actionButton;

        [BindComponent(source: BindComponentSource.Children, tag: "Delete", isRequired: false)]
        protected PressableButton deleteButton;

        public AudioNoteData Data { get; set; }

        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;

            if (content == null)
            {
                this.content = this.Owner.FindChildrenByTag("Content").FirstOrDefault();
            }

            if (actionButton == null)
            {
                this.actionButton = this.Owner.FindChildrenByTag("ContentAction", isRecursive:true).FirstOrDefault().FindComponentInChildren<PressableButton>();
            }

            this.ShowContent(false);

            deleteButton.ButtonReleased += DeleteButton_ButtonReleased;
            actionButton.ButtonReleased += ActionButton_ButtonReleased;

            return true;
        }

        protected virtual void ActionButton_ButtonReleased(object sender, EventArgs e)
        {
            this.ShowContent(!this.content.IsEnabled);
        }

        private void DeleteButton_ButtonReleased(object sender, EventArgs e)
        {
            this.xrvService.PubSub.Publish(new AudioNoteDeleteMessage()
            {
                Data = this.Data
            });
        }

        protected void ShowContent(bool show)
        {
            this.content.IsEnabled = show;
        }
    }
}
