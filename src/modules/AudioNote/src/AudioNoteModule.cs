// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.AudioNote.Services;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.AudioNote
{
    /// <summary>
    /// Audio note module for recording and playing auidio notes.
    /// </summary>
    public class AudioNoteModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private HandMenuButtonDescription handMenuDesc;
        private TabItem help;
        private Entity audioNoteHelp;
        private Window window;
        private Scene scene;
        private AudioNoteAnchor lastAnchorSelected;
        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private AudioNoteDeleteMessage audionoteToRemove;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioNoteModule"/> class.
        /// Audio note module for recording and playing auidio notes.
        /// </summary>
        public AudioNoteModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
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
        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

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

            this.window = this.ShowAudionoteWindow(AudioNoteResourceIDs.Prefabs.Window);
            this.window.Closed += this.Window_Closed;

            this.xrv.PubSub.Subscribe<AudioAnchorSelectedMessage>(this.CreateAudioNoteWindow);
            this.xrv.PubSub.Subscribe<AudioNoteDeleteMessage>(this.ConfirmDelete);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            var anchor = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Anchor).Instantiate();

            this.SetFrontPosition(this.scene, anchor);
            this.AddAudioAnchor(anchor);

            this.xrv.PubSub.Publish(new AudioAnchorSelectedMessage()
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
            var confirmDelete = this.xrv.WindowSystem.ShowConfirmDialog("Delete this note?", "This action can’t be undone.", "No", "Yes");

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
