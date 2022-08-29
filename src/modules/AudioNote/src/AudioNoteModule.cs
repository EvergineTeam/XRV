using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.AudioNote
{
    public class AudioNoteModule : Module
    {
        public override string Name => "AudioNote";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        public override TabItem Help => this.help;

        public override TabItem Settings => this.settings;

        protected AssetsService assetsService;
        private XrvService xrv;
        private HandMenuButtonDescription handMenuDesc;
        private TabItem settings;
        private TabItem help;

        private Entity audioNoteHelp;
        private Entity audioNoteSettings;
        private Window window;
        private Scene scene;
        private AudioNoteAnchor lastAnchorSelected;
        private AudioNoteWindow windowAudioNote;

        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private AudioNoteDeleteMessage audionoteToRemove;

        public AudioNoteModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
            {
                IconOn = AudioNoteResourceIDs.Materials.Icons.AudioNote,
                IsToggle = false,
                TextOn = "Audio Note",
            };

            this.settings = new TabItem()
            {
                Name = "Audio Note",
                Contents = SettingContent,
            };

            this.help = new TabItem()
            {
                Name = "Audio Note",
                Contents = HelpContent,
            };
        }

        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            // Settings
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Settings);
            this.audioNoteSettings = rulerSettingPrefab.Instantiate();

            this.window = this.ShowAudionoteWindow(AudioNoteResourceIDs.Prefabs.Window);

            this.xrv.PubSub.Subscribe<AudioAnchorSelected>(this.CreateAudioNoteWindow);
            this.xrv.PubSub.Subscribe<AudioNoteDeleteMessage>(this.ConfirmDelete);
        }

        public override void Run(bool turnOn)
        {
            var anchor = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Anchor).Instantiate();

            this.SetFrontPosition(this.scene, anchor);
            this.AddAudioAnchor(anchor);

            this.xrv.PubSub.Publish(new AudioAnchorSelected()
            {
                Anchor = anchor.FindComponent<AudioNoteAnchor>(),
            });
        }

        private void AddAudioAnchor(Entity anchor)
        {
            var c = anchor.FindComponent<AudioNoteAnchor>();
            if (c != null)
            {
                this.scene.Managers.EntityManager.Add(anchor);
                anchorsDic.Add(c.AudioNote.Guid, anchor);
            }
        }

        public Vector3 GetFrontPosition(Scene scene)
        {
            var cameraTransform = scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            // TODO uses NEAR position instead of 0.6f
            return cameraTransform.Position + cameraWorldTransform.Forward * 0.6f;
        }

        public void SetFrontPosition(Scene scene, Entity entity)
        {
            var anchorTransform = entity.FindComponent<Transform3D>();
            anchorTransform.Position = this.GetFrontPosition(scene);
        }

        private Entity SettingContent()
        {
            return this.audioNoteSettings;
        }

        private Entity HelpContent()
        {
            if (this.audioNoteHelp == null)
            {
                var rulerHelpPrefab = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Help);
                this.audioNoteHelp = rulerHelpPrefab.Instantiate();
            }

            return this.audioNoteHelp;
        }

        private void CreateAudioNoteWindow(AudioAnchorSelected msg)
        {
            this.window.Open();
            if (this.windowAudioNote == null)
            {
                this.windowAudioNote = this.window.Owner.FindComponentInChildren<AudioNoteWindow>();
            }

            if (this.windowAudioNote.WindowState == AudioNoteWindowState.Recording)
            {
                this.windowAudioNote.SaveContent();
            }
            else if (this.windowAudioNote.WindowState == AudioNoteWindowState.Recording)
            {
                this.windowAudioNote.PlayAudio(false);
            }

            if (lastAnchorSelected != null)
            {
                this.lastAnchorSelected.UpdateVisualState(AudioNoteAnchorVisual.Idle);
                this.lastAnchorSelected.IsSelected = false;
            }

            msg.Anchor.UpdateVisualState(AudioNoteAnchorVisual.Selected);
            msg.Anchor.IsSelected = true;
            this.lastAnchorSelected = msg.Anchor;

            this.SetWindowInitialState(msg.Anchor.AudioNote);
        }

        private void SetWindowInitialState(AudioNoteData data)
        {
            var note = this.window.Owner.FindComponentInChildren<AudioNoteWindow>();
            note.Data = data;
            this.window.Open();
            if (string.IsNullOrEmpty(data.Path))
            {
                note.WindowState = AudioNoteWindowState.Recording;
            }
            else
            {
                note.WindowState = AudioNoteWindowState.Playing;
            }
        }

        private void ConfirmDelete(AudioNoteDeleteMessage msg)
        {
            var confirmDelete = this.xrv.WindowSystem.ShowConfirmDialog("Delete this note?", "This action can’t be undone.", "No", "Yes");

            confirmDelete.Open();
            this.audionoteToRemove = msg;
            confirmDelete.Closed += Alert_Closed;
        }

        private void Alert_Closed(object sender, System.EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Alert_Closed;
                var audioNote = this.audionoteToRemove;
                this.audionoteToRemove = null;

                var isAcceted = dialog.Result == ConfirmDialog.AcceptKey;
                if (!isAcceted) return;

                var guid = audioNote?.Data.Guid;
                if (string.IsNullOrEmpty(guid)) return;

                this.RemoveAnchor(guid);
                this.window.Close();
            }
        }

        public void RemoveAnchor(string guid)
        {
            this.lastAnchorSelected = null;

            // TODO when anchor is serialized, remove from there also
            if (this.anchorsDic.TryGetValue(guid, out var anchor))
            {
                this.scene.Managers.EntityManager.Remove(anchor);
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

            return window;
        }
    }
}
