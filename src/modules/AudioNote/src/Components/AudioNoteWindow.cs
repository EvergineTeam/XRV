﻿using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Linq;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.Core;
using Xrv.Core.UI.Dialogs;

namespace Xrv.AudioNote
{
    public enum AudioNoteWindowState
    {
        None,
        Recording,
        Playing,
        StopPlaying,
        StopRecording,
        ReadyToPlay,
    }

    public class AudioNoteWindow : Component
    {
        private AudioNoteWindowState windowState;
        private AudioNoteData data;


        [BindService]
        protected XrvService xrvService;

        [BindEntity(source: BindEntitySource.Children, tag: "Recording")]
        protected Entity recordingEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "Playing")]
        protected Entity playingEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "Record", isRecursive: true)]
        protected Entity recordEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "Play", isRecursive: true)]
        protected Entity playEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "Delete", isRecursive: true)]
        protected Entity deleteEntity;

        protected PressableButton playButton;
        protected PressableButton recordButton;
        protected PressableButton deleteButton;
        protected ToggleStateManager recordManager;
        protected ToggleStateManager playManager;

        public AudioNoteData Data { get => data; set => data = value; }

        public AudioNoteWindowState WindowState
        {
            get => windowState;
            set
            {
                windowState = value;
                if (this.IsAttached)
                {
                    this.UpdateWindowState(windowState);
                }
            }
        }

        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;

            this.playButton = this.playEntity.FindComponentInChildren<PressableButton>();
            this.recordButton = this.recordEntity.FindComponentInChildren<PressableButton>();
            this.deleteButton = this.deleteEntity.FindComponentInChildren<PressableButton>();

            this.playManager = this.playEntity.FindComponentInChildren<ToggleStateManager>();
            this.recordManager = this.recordEntity.FindComponentInChildren<ToggleStateManager>();

            this.playingEntity.IsEnabled = false;
            this.recordingEntity.IsEnabled = false;

            this.deleteButton.ButtonReleased += this.DeleteButton_ButtonReleased;
            this.playButton.ButtonReleased += this.PlayButton_ButtonReleased;
            this.recordButton.ButtonReleased += this.RecordButton_ButtonReleased;

            return true;
        }

        private void RecordButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.windowState == AudioNoteWindowState.Recording)
            {
                this.WindowState = AudioNoteWindowState.StopRecording;
            }
            else if (this.windowState == AudioNoteWindowState.Playing)
            {
                this.WindowState = AudioNoteWindowState.Playing;
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(this.data.Path))
                {
                    var confirmOverride = this.xrvService.WindowSystem.ShowConfirmDialog("Override this audio?", "This action can’t be undone.", "No", "Yes");
                    confirmOverride.Closed += ConfirmOverride_Closed;
                    confirmOverride.Open();
                }
                else
                {
                    this.WindowState = AudioNoteWindowState.Recording;
                }
            }
        }

        private void PlayButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.windowState == AudioNoteWindowState.Playing)
            {
                this.WindowState = AudioNoteWindowState.StopPlaying;
            }
            else if (this.windowState == AudioNoteWindowState.Recording)
            {
                this.WindowState = AudioNoteWindowState.Recording;
                return;
            }
            else
            {
                this.WindowState = AudioNoteWindowState.Playing;
            }
        }

        private void ConfirmOverride_Closed(object sender, EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.ConfirmOverride_Closed;

                var isAcceted = dialog.Result == ConfirmDialog.AcceptKey;

                if (isAcceted)
                {
                    this.WindowState = AudioNoteWindowState.Recording;
                }
                else
                {
                    this.WindowState = AudioNoteWindowState.ReadyToPlay;
                }
            }
        }

        private void DeleteButton_ButtonReleased(object sender, EventArgs e)
        {
            this.xrvService.PubSub.Publish(new AudioNoteDeleteMessage()
            {
                Data = this.Data
            });
        }

        private void UpdateWindowState(AudioNoteWindowState windowState)
        {
            WorkActionFactory.CreateDelayWorkAction(this.Owner.Scene, TimeSpan.FromSeconds(0.1f))
                .ContinueWithAction(() =>
                {
                    this.playingEntity.IsEnabled = windowState == AudioNoteWindowState.Playing;
                    this.recordingEntity.IsEnabled = windowState == AudioNoteWindowState.Recording;
                    switch (windowState)
                    {
                        case AudioNoteWindowState.Recording:
                            this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                            this.recordManager.ChangeState(this.playManager.States.ElementAt(0));
                            this.BeginRecordAudionote();
                            break;
                        case AudioNoteWindowState.Playing:
                            this.playManager.ChangeState(this.playManager.States.ElementAt(0));
                            this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                            break;
                        case AudioNoteWindowState.StopPlaying:
                            this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                            this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                            break;
                        case AudioNoteWindowState.StopRecording:
                            this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                            this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                            this.SaveContent();
                            break;
                        case AudioNoteWindowState.ReadyToPlay:
                            this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                            this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                            break;
                        default:
                            break;
                    }
                })
                .Run();
        }

        private void BeginRecordAudionote()
        {
            // TODO begin record            
        }

        public void SaveContent()
        {
            // TODO stop record
            // TODO do save content here
            if (!string.IsNullOrEmpty(this.Data.Path))
            {
                // TODO remove previous record
            }

            this.Data.Path = Guid.NewGuid().ToString();
        }

        public void PlayAudio(bool play = true)
        {
            // TODO stop playing
        }
    }
}
