// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using Evergine.Framework;
using Evergine.MRTK.Emulation;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Extensions;
using Xrv.Core.Help;
using Xrv.Core.Menu;
using Xrv.Core.Settings;

namespace Xrv.Core.VoiceCommands
{
    internal class VoiceCommandsSystem
    {
        private IVoiceCommandService voiceService = null;
        private HashSet<string> keyWords;

        public VoiceCommandsSystem()
        {
            this.keyWords = new HashSet<string>();
        }

        public void RegisterService()
        {
#if UWP
            this.voiceService = new Evergine.MRTK.Services.Voice.VoiceCommandService();
#endif

            if (this.voiceService != null)
            {
                Application.Current.Container.RegisterInstance(this.voiceService);
            }
        }

        public void Load()
        {
            // Hand menu voice commands
            this.keyWords.Add(HandMenu.VoiceCommands.DetachMenu);
            this.keyWords.Add(SettingsSystem.VoiceCommands.ShowSettings);
            this.keyWords.Add(HelpSystem.VoiceCommands.ShowHelp);
        }

        public void RegisterCommands(IEnumerable<string> voiceCommands) =>
            this.keyWords.AddRange(voiceCommands);

        public void Initialize() =>
            this.voiceService?.ConfigureVoiceCommands(this.keyWords.ToArray());
    }
}
