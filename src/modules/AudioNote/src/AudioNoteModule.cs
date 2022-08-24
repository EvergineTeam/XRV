using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.Modules;
using Xrv.Core;
using Xrv.AudioNote.Messages;
using Evergine.Framework.Graphics;
using System.Diagnostics;

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

        private List<Entity> audioAnchorList = new List<Entity>();

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

            // Audio Note

            // Settings
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Settings);
            this.audioNoteSettings = rulerSettingPrefab.Instantiate();

            this.xrv.PubSub.Subscribe<AudioNoteMessage>(this.CreateAudioNoteWindow);
        }

        public override void Run(bool turnOn)
        {
            var anchor = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.Anchor).Instantiate();

            var anchorTransform = anchor.FindComponent<Transform3D>();
            var cameraTransform = this.scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            anchorTransform.Position = cameraTransform.Position + cameraWorldTransform.Forward * 0.6f;
            this.AddAudioAnchor(anchor);
        }

        private void AddAudioAnchor(Entity anchor)
        {
            this.scene.Managers.EntityManager.Add(anchor);
            audioAnchorList.Add(anchor);
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

        private void CreateAudioNoteWindow(AudioNoteMessage obj)
        {
            Debug.WriteLine("Touch detected");
        }
    }
}
