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

        [BindEntity(source: BindEntitySource.Children, tag: "Content")]
        protected Entity content;

        [BindEntity(source: BindEntitySource.Children, tag: "ContentAction", isRecursive: true)]
        protected Entity contentActionEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "Delete", isRecursive:true)]
        protected Entity deleteEntity;

        protected PressableButton actionButton;
        protected PressableButton deleteButton;

        public AudioNoteData Data { get; set; }

        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;

            this.actionButton = this.contentActionEntity.FindComponentInChildren<PressableButton>();
            this.deleteButton = this.deleteEntity.FindComponentInChildren<PressableButton>();

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
