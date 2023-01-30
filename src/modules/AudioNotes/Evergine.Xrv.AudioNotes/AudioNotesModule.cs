// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Evergine.Xrv.AudioNotes.Messages;
using Evergine.Xrv.AudioNotes.Models;
using Evergine.Xrv.AudioNotes.Services;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.UI.Dialogs;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.AudioNotes
{
    /// <summary>
    /// Audio note module for recording and playing auidio notes.
    /// </summary>
    public class AudioNotesModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private Entity audioNoteHelp;
        private Window window;
        private Scene scene;
        private AudioNoteAnchor lastAnchorSelected;
        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private AudioNoteDeleteMessage audionoteToRemove;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioNotesModule"/> class.
        /// Audio note module for recording and playing auidio notes.
        /// </summary>
        public AudioNotesModule()
        {
            Application.Current.Container.RegisterInstance(new PlaybackService());
            Application.Current.Container.RegisterInstance(new RecordingService());
        }

        /// <inheritdoc/>
        public override string Name => "AudioNote";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <summary>
        /// Removes anchor from scene.
        /// </summary>
        /// <param name="guid">Anchor guid to remove.</param>
        public void RemoveAnchor(string guid)
        {
            this.lastAnchorSelected = null;

            // TODO when anchor is serialized, remove from there also
            if (this.anchorsDic.TryGetValue(guid, out var anchor))
            {
                this.scene.Managers.EntityManager.Remove(anchor);
            }
        }

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOn = AudioNotesResourceIDs.Materials.Icons.AudioNote,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.Help = new TabItem()
            {
                Name = () => this.xrv.Localization.GetString(() => Resources.Strings.Help_Tab_Name),
                Contents = this.HelpContent,
            };

            this.window = this.ShowAudionoteWindow(AudioNotesResourceIDs.Prefabs.Window);
            this.window.Closed += this.Window_Closed;

            this.xrv.Services.Messaging.Subscribe<AudioAnchorSelectedMessage>(this.CreateAudioNoteWindow);
            this.xrv.Services.Messaging.Subscribe<AudioNoteDeleteMessage>(this.ConfirmDelete);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            var anchor = this.assetsService.Load<Prefab>(AudioNotesResourceIDs.Prefabs.Anchor).Instantiate();

            this.SetFrontPosition(this.scene, anchor);
            this.AddAudioAnchor(anchor);

            this.xrv.Services.Messaging.Publish(new AudioAnchorSelectedMessage()
            {
                Anchor = anchor.FindComponent<AudioNoteAnchor>(),
            });
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
            }
        }

        private void SetFrontPosition(Scene scene, Entity entity)
        {
            var anchorTransform = entity.FindComponent<Transform3D>();
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            anchorTransform.Position = cameraTransform.Position + (cameraWorldTransform.Forward * this.xrv.WindowsSystem.Distances.Far);
        }

        private Entity HelpContent()
        {
            if (this.audioNoteHelp == null)
            {
                var audioHelpPrefab = this.assetsService.Load<Prefab>(AudioNotesResourceIDs.Prefabs.Help);
                this.audioNoteHelp = audioHelpPrefab.Instantiate();
            }

            return this.audioNoteHelp;
        }

        private void CreateAudioNoteWindow(AudioAnchorSelectedMessage msg)
        {
            this.Window_Closed(this, EventArgs.Empty);
            this.window.Open();

            msg.Anchor.UpdateVisualState(AudioNoteAnchorVisual.Selected);
            msg.Anchor.IsSelected = true;
            this.lastAnchorSelected = msg.Anchor;

            _ = this.SetWindowInitialState(msg.Anchor.AudioNote);
        }

        private async Task<bool> SetWindowInitialState(AudioNoteData data)
        {
            var ok = true;
            var note = this.window.Owner.FindComponentInChildren<AudioNoteWindow>();

            if (note.WindowState == AudioNoteWindowState.Recording)
            {
                ok = await note.StopRecordingAsync();
            }

            note.Data = data;
            this.window.Open();
            if (string.IsNullOrEmpty(data.Path))
            {
                ok &= await note.StartRecordingAsync();
            }
            else
            {
                ok &= await note.StartPlayingAsync();
            }

            return ok;
        }

        private void ConfirmDelete(AudioNoteDeleteMessage msg)
        {
            var confirmDelete = this.xrv.WindowsSystem.ShowConfirmationDialog("Delete this note?", "This action can't be undone.", "No", "Yes");

            confirmDelete.Open();
            this.audionoteToRemove = msg;
            confirmDelete.Closed += this.Alert_Closed;
        }

        private void Alert_Closed(object sender, System.EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Alert_Closed;
                var audioNote = this.audionoteToRemove;
                this.audionoteToRemove = null;

                var isAcceted = dialog.Result == ConfirmationDialog.AcceptKey;
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
            }
        }

        private Window ShowAudionoteWindow(Guid prefabId)
        {
            var audioNoteSize = new Vector2(0.18f, 0.04f);
            var window = this.xrv.WindowsSystem.CreateWindow((config) =>
            {
                config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
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
