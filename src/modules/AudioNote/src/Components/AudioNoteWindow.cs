// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.AudioNote.Services;
using Xrv.Core;
using Xrv.Core.UI.Dialogs;

namespace Xrv.AudioNote
{
    /// <summary>
    /// Audio note window state enum.
    /// </summary>
    public enum AudioNoteWindowState
    {
        /// <summary>
        /// None enum.
        /// </summary>
        None,

        /// <summary>
        /// Recording enum.
        /// </summary>
        Recording,

        /// <summary>
        /// Playing enum.
        /// </summary>
        Playing,

        /// <summary>
        /// Stop playing enum.
        /// </summary>
        StopPlaying,

        /// <summary>
        /// Stop recording enum.
        /// </summary>
        StopRecording,

        /// <summary>
        /// Ready to play enum.
        /// </summary>
        ReadyToPlay,
    }

    /// <summary>
    /// Audio note window Component.
    /// </summary>
    public class AudioNoteWindow : Component
    {
        /// <summary>
        /// Xrv service.
        /// </summary>
        [BindService]
        protected XrvService xrvService;

        /// <summary>
        /// Playback service.
        /// </summary>
        [BindService]
        protected PlaybackService playbackService;

        /// <summary>
        /// Recording service.
        /// </summary>
        [BindService]
        protected RecordingService recordingService;

        /// <summary>
        /// Recording entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Recording")]
        protected Entity recordingEntity;

        /// <summary>
        /// Playing entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Playing")]
        protected Entity playingEntity;

        /// <summary>
        /// Record entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Record", isRecursive: true)]
        protected Entity recordEntity;

        /// <summary>
        /// Play entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Play", isRecursive: true)]
        protected Entity playEntity;

        /// <summary>
        /// Delete entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Delete", isRecursive: true)]
        protected Entity deleteEntity;

        /// <summary>
        /// Time text3DMesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "Time")]
        protected Text3DMesh recordedTimeText;

        /// <summary>
        /// Total text3DMesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "Total")]
        protected Text3DMesh playTotalText;

        /// <summary>
        /// Current text3DMesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "Current")]
        protected Text3DMesh playCurrentText;

        /// <summary>
        /// PlayingPlate material component.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PlayingPlate")]
        protected MaterialComponent playingPlate;

        private AudioNoteWindowState windowState;
        private AudioNoteData data;
        private PressableButton playButton;
        private PressableButton recordButton;
        private PressableButton deleteButton;
        private ToggleStateManager recordManager;
        private ToggleStateManager playManager;
        private HoloGraphic playingMaterial;

        /// <summary>
        /// Gets or sets data.
        /// </summary>
        public AudioNoteData Data { get => this.data; set => this.data = value; }

        /// <summary>
        /// Gets or sets windowState.
        /// </summary>
        public AudioNoteWindowState WindowState
        {
            get => this.windowState;
            set
            {
                this.windowState = value;
                if (this.IsAttached)
                {
                    this.UpdateWindowState(this.windowState);
                }
            }
        }

        /// <summary>
        /// Start recording async.
        /// </summary>
        /// <returns>true if ok.</returns>
        public async Task<bool> StartRecordingAsync()
        {
            var ok = true;
            try
            {
                if (this.windowState == AudioNoteWindowState.Playing)
                {
                    this.StopPlaying();
                }

                this.WindowState = AudioNoteWindowState.Recording;
                ok = await this.StartRecordingServiceAsync();
                if (!ok)
                {
                    this.xrvService.WindowsSystem.ShowAlertDialog("Audio note error", "Cannot record audio.", "Ok");
                    this.WindowState = AudioNoteWindowState.ReadyToPlay;
                }
            }
            catch (Exception ex)
            {
                this.xrvService.WindowsSystem.ShowAlertDialog("Audio note recording error", $"{ex.Message}", "Ok");
                return false;
            }

            return ok;
        }

        /// <summary>
        /// Start playing async.
        /// </summary>
        /// <returns>true if ok.</returns>
        public async Task<bool> StartPlayingAsync()
        {
            var ok = true;
            try
            {
                if (this.windowState == AudioNoteWindowState.Recording)
                {
                    var stream = await this.StopRecordingServiceAsync();
                    if (stream != null)
                    {
                        await this.SaveContentAsync(stream);
                    }
                    else
                    {
                        this.xrvService.WindowsSystem.ShowAlertDialog("Audio note save error", "Cannot save audio.", "Ok");
                        this.WindowState = AudioNoteWindowState.ReadyToPlay;
                        return false;
                    }
                }

                this.StopPlaying();
                this.WindowState = AudioNoteWindowState.Playing;
                ok = await this.StartPlayingServiceAsync();
                if (!ok)
                {
                    this.xrvService.WindowsSystem.ShowAlertDialog("Audio note error", "Cannot reproduce audio.", "Ok");
                    this.WindowState = AudioNoteWindowState.ReadyToPlay;
                }
            }
            catch (Exception ex)
            {
                this.xrvService.WindowsSystem.ShowAlertDialog("Audio note playing error", $"{ex.Message}", "Ok");
                return false;
            }

            return ok;
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        public void StopPlaying()
        {
            this.WindowState = AudioNoteWindowState.ReadyToPlay;
            if (this.playbackService.IsPlaying)
            {
                this.playbackService.Stop();
            }
        }

        /// <summary>
        /// Stop recording async.
        /// </summary>
        /// <param name="save">if true also saves content.</param>
        /// <returns>true if ok.</returns>
        public async Task<bool> StopRecordingAsync(bool save = true)
        {
            var ok = true;
            if (this.recordingService.IsRecording)
            {
                var stream = await this.recordingService.StopRecordingAsync();
                if (save)
                {
                    ok &= await this.SaveContentAsync(stream);
                }
            }

            this.WindowState = AudioNoteWindowState.ReadyToPlay;

            if (!ok)
            {
                this.xrvService.WindowsSystem.ShowAlertDialog("Audio note error", "Audio note not saved.", "Ok");
            }

            return ok;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            if (Application.Current.IsEditor)
            {
                return true;
            }

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

            this.playbackService.OnPositionChanged += this.PlaybackService_CurrentPosition;
            this.playbackService.OnAudioEnd += this.PlaybackService_AudioEnd;

            this.recordingService.OnRecordingTime += this.RecordingService_RecordingTimeChanged;
            this.playingMaterial = new HoloGraphic(this.playingPlate.Material);

            return true;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            if (Application.Current.IsEditor)
            {
                return;
            }

            this.deleteButton.ButtonReleased -= this.DeleteButton_ButtonReleased;
            this.playButton.ButtonReleased -= this.PlayButton_ButtonReleased;
            this.recordButton.ButtonReleased -= this.RecordButton_ButtonReleased;
        }

        private void RecordingService_RecordingTimeChanged(object sender, TimeSpan e)
        {
            this.recordedTimeText.Text = e.ToString("mm\\:ss");
        }

        private void PlaybackService_AudioEnd(object sender, EventArgs e)
        {
            this.WindowState = AudioNoteWindowState.ReadyToPlay;
        }

        private void UpdatePlayProgress(float progres)
        {
            this.playingMaterial.Parameters_Offset = new Vector2(MathHelper.Lerp(0.5f, -0.5f, progres), 0);
        }

        private void PlaybackService_CurrentPosition(object sender, TimeSpan e)
        {
            this.playCurrentText.Text = e.ToString("mm\\:ss");
            this.playTotalText.Text = this.playbackService.Duration.ToString("mm\\:ss");

            var progres = MathHelper.Lerp(0, 1, (float)(e.TotalMilliseconds / this.playbackService.Duration.TotalMilliseconds));
            this.UpdatePlayProgress(progres);
        }

        private void RecordButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.windowState == AudioNoteWindowState.Recording)
            {
                _ = this.StopRecordingAsync();
            }
            else
            {
                if (!string.IsNullOrEmpty(this.data.Path))
                {
                    var confirmOverride = this.xrvService.WindowsSystem.ShowConfirmationDialog("Override this audio?", "This action can’t be undone.", "No", "Yes");
                    confirmOverride.Closed += this.ConfirmOverride_Closed;
                    confirmOverride.Open();
                }
                else
                {
                    _ = this.StartRecordingAsync();
                }
            }
        }

        private void PlayButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.windowState == AudioNoteWindowState.Playing)
            {
                this.StopPlaying();
            }
            else
            {
                _ = this.StartPlayingAsync();
            }
        }

        private async void ConfirmOverride_Closed(object sender, EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.ConfirmOverride_Closed;

                var isAcceted = dialog.Result == ConfirmationDialog.AcceptKey;

                if (isAcceted)
                {
                    await this.StartRecordingAsync();
                }
                else
                {
                    this.WindowState = AudioNoteWindowState.ReadyToPlay;
                }
            }
        }

        private void DeleteButton_ButtonReleased(object sender, EventArgs e)
        {
            this.StopPlaying();
            _ = this.StopRecordingAsync(false);

            this.xrvService.PubSub.Publish(new AudioNoteDeleteMessage()
            {
                Data = this.Data,
                Window = this,
            });
        }

        private void UpdateWindowState(AudioNoteWindowState windowState)
        {
            this.playingEntity.IsEnabled = windowState == AudioNoteWindowState.Playing;
            this.recordingEntity.IsEnabled = windowState == AudioNoteWindowState.Recording;

            this.playButton.IsEnabled = windowState != AudioNoteWindowState.Recording;
            this.recordButton.IsEnabled = windowState != AudioNoteWindowState.Playing;
            switch (windowState)
            {
                case AudioNoteWindowState.Recording:
                    this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                    this.recordManager.ChangeState(this.playManager.States.ElementAt(0));
                    break;
                case AudioNoteWindowState.Playing:
                    this.playManager.ChangeState(this.playManager.States.ElementAt(0));
                    this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                    break;
                default:
                    this.playManager.ChangeState(this.playManager.States.ElementAt(1));
                    this.recordManager.ChangeState(this.playManager.States.ElementAt(1));
                    break;
            }
        }

        private async Task<bool> StartRecordingServiceAsync()
        {
            return await this.recordingService.StartRecordingAsync();
        }

        private async Task<Stream> StopRecordingServiceAsync()
        {
            if (this.recordingService.IsRecording)
            {
                return await this.recordingService.StopRecordingAsync();
            }

            return null;
        }

        private async Task<bool> StartPlayingServiceAsync()
        {
            try
            {
                // TODO read real file this.data.Path
                var stream = new MemoryStream();

                await this.playbackService.Load(stream);
                this.playbackService.Play();
            }
            catch (Exception ex)
            {
                this.xrvService.WindowsSystem.ShowAlertDialog("Audio note playing error", $"{ex.Message}", "Ok");
                return false;
            }

            return true;
        }

        private async Task<bool> SaveContentAsync(Stream stream)
        {
            // TODO do save content here
            await Task.Delay(1);

            if (!string.IsNullOrEmpty(this.Data.Path))
            {
                // TODO remove previous record
            }

            this.Data.Path = "XRV/Audio/sample.wav";

            return true;
        }
    }
}
