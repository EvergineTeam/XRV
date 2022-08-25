using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using Xrv.AudioNote.Messages;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Tabs;

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

        private Scene scene;

        private Dictionary<string, Entity> anchorsDic = new Dictionary<string, Entity>();
        private Dictionary<string, Entity> windowsDic = new Dictionary<string, Entity>();
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

            this.xrv.PubSub.Subscribe<AudioNoteMessage>(this.CreateAudioNoteWindow);
            this.xrv.PubSub.Subscribe<AudioNoteDeleteMessage>(this.CreateAudioNoteDeleteWindow);
        }

        public override void Run(bool turnOn)
        {
            var anchor = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Anchor).Instantiate();

            this.SetFrontPosition(this.scene, anchor);
            this.AddAudioAnchor(anchor);
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

        private void CreateAudioNoteWindow(AudioNoteMessage msg)
        {
            if (!this.windowsDic.TryGetValue(msg.Data.Guid, out var note))
            {
                if (string.IsNullOrEmpty(msg.Data.Path))
                {
                    note = this.ShowEmptyAudionote(msg);
                }
                else
                {
                    note = this.ShowRecordedAudionote(msg);
                }

                this.windowsDic.Add(msg.Data.Guid, note);
            }

            this.SetFrontPosition(this.scene, note);
        }

        private void CreateAudioNoteDeleteWindow(AudioNoteDeleteMessage msg)
        {
            var alert = this.xrv.WindowSystem.ShowConfirmDialog("Delete this note?", "This action can’t be undone.", "No", "Yes");
            alert.Open();
            this.audionoteToRemove = msg;
            alert.Closed += Alert_Closed;
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
            }
        }

        public void RemoveAnchor(string guid)
        {
            if (this.anchorsDic.TryGetValue(guid, out var anchor))
            {
                this.scene.Managers.EntityManager.Remove(anchor);
            }

            this.RemoveWindow(guid);
        }

        public void RemoveWindow(string guid)
        {
            if (this.windowsDic.TryGetValue(guid, out var window))
            {
                this.scene.Managers.EntityManager.Remove(window);
            }
        }

        public Entity ShowRecordedAudionote(AudioNoteMessage message)
        {
            return ShowAudionoteWindow(message, AudioNoteResourceIDs.Prefabs.Recorded);
        }

        public Entity ShowEmptyAudionote(AudioNoteMessage message)
        {
            return ShowAudionoteWindow(message, AudioNoteResourceIDs.Prefabs.Empty);
        }

        private Entity ShowAudionoteWindow(AudioNoteMessage message, Guid prefabId)
        {
            var audioNoteSize = new Vector2(0.15f, 0.04f);
            var window = this.xrv.WindowSystem.ShowWindow();
            var config = window.Configurator;
            config.Title = "Audio Note";
            config.Size = audioNoteSize;
            config.FrontPlateSize = audioNoteSize;
            config.FrontPlateOffsets = Vector2.Zero;
            config.DisplayLogo = false;
            config.Content = this.assetsService.Load<Prefab>(prefabId).Instantiate();

            var audionoteBase = config.Content.FindComponent<AudioNoteBase>(isExactType: false);
            audionoteBase.Data = message.Data;
            window.Open();

            return window.Owner;
        }
    }
}
