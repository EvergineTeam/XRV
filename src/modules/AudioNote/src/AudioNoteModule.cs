using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using Xrv.Core.Menu;
using Xrv.Core.UI.Tabs;
using Xrv.Core.Modules;

namespace Xrv.AudioNote
{
    public class AudioNoteModule : Module
    {
        public override string Name => "AudioNote";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        public override TabItem Help => this.help;

        public override TabItem Settings => this.settings;

        protected AssetsService assetsService;
        private HandMenuButtonDescription handMenuDesc;
        private TabItem settings;
        private TabItem help;

        private Entity audioNoteHelp;
        private Entity audioNoteSettings;

        public AudioNoteModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
            {
                //IconOff = AudioNoteResourceIDs.Materials.Icons.Measure,
                //IconOn = AudioNoteResourceIDs.Materials.Icons.Measure,
                IsToggle = true,
                TextOn = "Hide",
                TextOff = "Show"
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

            // Audio Note
            
            // Settings
        }

        public override void Run(bool turnOn)
        {
            if (turnOn)
            {
                //this.rulerBehavior.Reset();
            }

            //this.rulerEntity.IsEnabled = turnOn;
        }

        private Entity SettingContent()
        {
            return this.audioNoteSettings;
        }

        private Entity HelpContent()
        {
            if (this.audioNoteHelp == null)
            {
                //var rulerHelpPrefab = this.assetsService.Load<Prefab>(AudioNoteResourceIDs.Prefabs.RulerHelp_weprefab);
                //this.audioNoteHelp = rulerHelpPrefab.Instantiate();
            }

            return this.audioNoteHelp;
        }
    }
}
