// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.AudioNote.Services;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.Storage;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Path = System.IO.Path;

namespace Xrv.AudioNote
{
    /// <summary>
    /// Audio note module for recording and playing auidio notes.
    /// </summary>
    public class AudioNoteModule : Module
    {
        /// <summary>
        /// Audio Notes folder name.
        /// </summary>
        public const string FOLDERNAME = "audionotes";

        /// <summary>
        /// JSON file name.
        /// </summary>
        public const string FILENAME = "audionotes.json";

        /// <summary>
        /// JSON file name.
        /// </summary>
        public const string ANCHORTAG = "anchor tag";

        private AssetsService assetsService;
        private XrvService xrv;
        private MenuButtonDescription handMenuDesc;
        private TabItem help;
        private Entity audioNoteHelp;
        private Window window;
        private Scene scene;
        private AudioNoteAnchor lastAnchorSelected;
        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private AudioNoteDeleteMessage audionoteToRemove;
        private string audioNoteFilePath;
        private ApplicationDataFileAccess fileAccess;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioNoteModule"/> class.
        /// Audio note module for recording and playing auidio notes.
        /// </summary>
        public AudioNoteModule()
        {
            this.handMenuDesc = new MenuButtonDescription()
            {
                IconOn = AudioNoteResourceIDs.Materials.Icons.AudioNote,
                IsToggle = false,
                TextOn = "Audio Note",
            };

            this.help = new TabItem()
            {
                Name = "Audio Note",
                Contents = this.HelpContent,
            };

            Application.Current.Container.RegisterInstance(new PlaybackService());
            Application.Current.Container.RegisterInstance(new RecordingService());
        }

        /// <inheritdoc/>
        public override string Name => "AudioNote";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => null;

        /// <summary>
        /// Removes anchor from scene.
        /// </summary>
        /// <param name="guid">Anchor guid to remove.</param>
        public void RemoveAnchor(string guid)
        {
            this.lastAnchorSelected = null;

            if (this.anchorsDic.TryGetValue(guid, out var anchor))
            {
                this.scene.Managers.EntityManager.Remove(anchor);
            }
        }

        /// <summary>
        /// Gets file from path.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>Stream if exist, null otherwise.</returns>
        public async Task<Stream> GetFileAsync(string path, CancellationToken cancellation = default)
        {
            if (!await this.fileAccess.ExistsFileAsync(path, cancellation))
            {
                return null;
            }

            return await this.fileAccess.GetFileAsync(path, cancellation);
        }

        /// <summary>
        /// Save audio file and update audionotes.json.
        /// </summary>
        /// <param name="stream">Audio note stream.</param>
        /// <param name="note">Audio note data.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>true if everything whent ok.</returns>
        public async Task<bool> SaveAudioFileAsync(Stream stream, AudioNoteData note, CancellationToken cancellation = default)
        {
            try
            {
                if (!await this.fileAccess.ExistsDirectoryAsync(FOLDERNAME, cancellation))
                {
                    await this.fileAccess.CreateDirectoryAsync(FOLDERNAME, cancellation);
                }

                var audionotePath = Path.Combine(FOLDERNAME, note.Guid);
                audionotePath += ".wav";
                note.Path = audionotePath;

                if (await this.fileAccess.ExistsFileAsync(audionotePath, cancellation))
                {
                    await this.fileAccess.DeleteFileAsync(audionotePath, cancellation);
                }

                // TODO store real audio
                stream = await this.fileAccess.GetFileAsync("audionotes\\smallSample.wav");

                await this.fileAccess.WriteFileAsync(audionotePath, stream, cancellation);
                await this.SerializeAudioNotesAsync();
            }
            catch (Exception ex)
            {
                this.xrv.WindowSystem.ShowAlertDialog("Audio note saving error", $"{ex.Message}", "Ok");
                return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override async void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            this.window = this.ShowAudionoteWindow(AudioNoteResourceIDs.Prefabs.Window);
            this.window.Closed += this.Window_Closed;

            this.xrv.PubSub.Subscribe<AudioAnchorSelectedMessage>(this.CreateAudioNoteWindow);
            this.xrv.PubSub.Subscribe<AudioNoteDeleteMessage>(this.ConfirmDelete);
            this.xrv.PubSub.Subscribe<SaveAnchorPositions>(this.SaveAnchorPositions);

            this.fileAccess = new ApplicationDataFileAccess();

            // Load previous audionotes
            await EvergineBackgroundTask.Run(async () =>
            {
                this.audioNoteFilePath = Path.Combine(FOLDERNAME, FILENAME);
                var list = new List<AudioNoteData>();

                if (await this.fileAccess.ExistsFileAsync(this.audioNoteFilePath))
                {
                    list = await JsonSerializer.DeserializeAsync<List<AudioNoteData>>(await this.fileAccess.GetFileAsync(this.audioNoteFilePath));
                }

                foreach (var note in list)
                {
                    var anchor = this.CreateAnchor();

                    anchor.transform.Position = note.GetPosition();
                    await EvergineForegroundTask.Run(() =>
                    {
                        scene.Managers.EntityManager.Add(anchor.entity);
                    });

                    anchor.note.AudioNote = note;
                }
            });
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            var anchor = this.CreateAnchor();

            this.SetFrontPosition(this.scene, anchor.transform);
            this.AddAudioAnchor(anchor.entity);

            this.xrv.PubSub.Publish(new AudioAnchorSelectedMessage()
            {
                Anchor = anchor.note,
            });
        }

        private (Entity entity, Transform3D transform, AudioNoteAnchor note) CreateAnchor()
        {
            var entity = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Anchor).Instantiate();
            entity.Tag = ANCHORTAG;
            var transform = entity.FindComponent<Transform3D>();
            var note = entity.FindComponent<AudioNoteAnchor>();

            return (entity, transform, note);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.lastAnchorSelected != null)
            {
                this.lastAnchorSelected.UpdateVisualState(AudioNoteAnchorVisual.Idle);
                this.lastAnchorSelected.IsSelected = false;
            }
        }

        private void AddAudioAnchor(Entity anchor)
        {
            var c = anchor.FindComponent<AudioNoteAnchor>();
            if (c != null)
            {
                this.scene.Managers.EntityManager.Add(anchor);
                this.anchorsDic.Add(c.AudioNote.Guid, anchor);

                var transform = anchor.FindComponent<Transform3D>();
                c.AudioNote.SetPosition(transform.Position);
            }
        }

        private async Task SerializeAudioNotesAsync()
        {
            await EvergineBackgroundTask.Run(async () =>
            {
                using (var stream = new MemoryStream())
                {
                    var anchors = this.scene.Managers.EntityManager.FindAllByTag(ANCHORTAG)
                    .Select(a => a.FindComponent<AudioNoteAnchor>().AudioNote)
                    .ToList();
                    await JsonSerializer.SerializeAsync(stream, anchors);
                    stream.Position = 0;
                    await this.fileAccess.WriteFileAsync(this.audioNoteFilePath, stream);
                }
            });
        }

        private void SetFrontPosition(Scene scene, Transform3D anchorTransform)
        {
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            anchorTransform.Position = cameraTransform.Position + (cameraWorldTransform.Forward * this.xrv.WindowSystem.Distances.Far);
        }

        private Entity HelpContent()
        {
            if (this.audioNoteHelp == null)
            {
                var audioHelpPrefab = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Help);
                this.audioNoteHelp = audioHelpPrefab.Instantiate();
            }

            return this.audioNoteHelp;
        }

        private async void CreateAudioNoteWindow(AudioAnchorSelectedMessage msg)
        {
            this.Window_Closed(this, EventArgs.Empty);
            this.window.Open();

            msg.Anchor.UpdateVisualState(AudioNoteAnchorVisual.Selected);
            msg.Anchor.IsSelected = true;
            this.lastAnchorSelected = msg.Anchor;

            var note = this.window.Owner.FindComponentInChildren<AudioNoteWindow>();

            if (note.WindowState == AudioNoteWindowState.Recording)
            {
                await note.StopRecordingAsync();
            }

            note.Data = msg.Anchor.AudioNote;
            this.window.Open();
            if (string.IsNullOrEmpty(note.Data.Path))
            {
                await note.StartRecordingAsync();
            }
            else
            {
                await note.StartPlayingAsync();
            }
        }

        private async void SaveAnchorPositions(SaveAnchorPositions msg)
        {
            await this.SerializeAudioNotesAsync();
        }

        private void ConfirmDelete(AudioNoteDeleteMessage msg)
        {
            var confirmDelete = this.xrv.WindowSystem.ShowConfirmDialog("Delete this note?", "This action can’t be undone.", "No", "Yes");

            confirmDelete.Open();
            this.audionoteToRemove = msg;
            confirmDelete.Closed += this.Alert_Closed;
        }

        private async void Alert_Closed(object sender, System.EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Alert_Closed;
                var audioNote = this.audionoteToRemove;
                this.audionoteToRemove = null;

                var isAcceted = dialog.Result == ConfirmDialog.AcceptKey;
                if (!isAcceted)
                {
                    return;
                }

                var guid = audioNote?.Data.Guid;
                if (string.IsNullOrEmpty(guid))
                {
                    return;
                }

                _ = audioNote.Window.StopRecordingAsync(false);
                this.RemoveAnchor(guid);
                this.window.Close();

                await this.SerializeAudioNotesAsync();
            }
        }

        private Window ShowAudionoteWindow(Guid prefabId)
        {
            var audioNoteSize = new Vector2(0.18f, 0.04f);
            var window = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.Title = "Audio Note";
                config.Size = audioNoteSize;
                config.FrontPlateSize = audioNoteSize;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = this.assetsService.Load<Prefab>(prefabId).Instantiate();
            });

            window.DistanceKey = Distances.NearKey;
            return window;
        }
    }
}
