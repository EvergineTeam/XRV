// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Networking.Components;
using System.Collections.Generic;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Ruler.Networking;

namespace Evergine.Xrv.Ruler
{
    /// <summary>
    /// Ruler module implementation.
    /// </summary>
    public class RulerModule : Module
    {
        private XrvService xrvService;
        private AssetsService assetsService;

        private Entity rulerEntity;
        private RulerBehavior rulerBehavior;
        private Entity rulerHelp;
        private Entity rulerSettings;

        /// <inheritdoc/>
        public override string Name => "Ruler";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => new[]
        {
            VoiceCommandsEntries.ShowRuler,
            VoiceCommandsEntries.HideRuler,
        };

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.xrvService = Application.Current.Container.Resolve<XrvService>();
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            // Menu, settings and help entries
            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOff = RulerResourceIDs.Materials.Icons.Measure,
                IconOn = RulerResourceIDs.Materials.Icons.Measure,
                IsToggle = true,
                TextOn = () => this.xrvService.Localization.GetString(() => Resources.Strings.Menu_Hide),
                TextOff = () => this.xrvService.Localization.GetString(() => Resources.Strings.Menu_Show),
                VoiceCommandOff = VoiceCommandsEntries.ShowRuler,
                VoiceCommandOn = VoiceCommandsEntries.HideRuler,
            };

            this.Settings = new TabItem()
            {
                Name = () => this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Tab_Name),
                Contents = this.SettingContent,
            };

            this.Help = new TabItem()
            {
                Name = () => this.xrvService.Localization.GetString(() => Resources.Strings.Help_Tab_Name),
                Contents = this.HelpContent,
            };

            // Ruler
            var rulerPrefab = this.assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.Ruler_weprefab);
            this.rulerEntity = rulerPrefab.Instantiate();
            this.rulerEntity.IsEnabled = false;

            var xrvService = Application.Current.Container.Resolve<XrvService>();
            xrvService.Networking.AddNetworkingEntity(this.rulerEntity);

            // Settings
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.RulerSettings_weprefab);
            this.rulerSettings = rulerSettingPrefab.Instantiate();

            // RulerBehavior
            this.rulerBehavior = this.rulerEntity.FindComponent<RulerBehavior>();
            this.rulerBehavior.Settings = this.rulerSettings;

            // Networking
            var networkingEntity = new Entity($"{nameof(RulerModule)}_Networking");
            var moduleActivationSync = new ModuleActivationSync
            {
                Module = this,
            };
            networkingEntity.AddComponent(new NetworkRoomProvider()); // TODO: try to hide this
            networkingEntity.AddComponent(moduleActivationSync);
            networkingEntity.AddComponent(new RulerSessionSynchronization());
            networkingEntity.AddComponent(new RulerModuleActivationKey());
            scene.Managers.EntityManager.Add(networkingEntity);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.rulerEntity.IsEnabled = turnOn;
        }

        private Entity SettingContent()
        {
            return this.rulerSettings;
        }

        private Entity HelpContent()
        {
            if (this.rulerHelp == null)
            {
                var rulerHelpPrefab = this.assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.RulerHelp_weprefab);
                this.rulerHelp = rulerHelpPrefab.Instantiate();
            }

            return this.rulerHelp;
        }

        private static class VoiceCommandsEntries
        {
            public const string ShowRuler = "Show ruler";

            public const string HideRuler = "Hide ruler";
        }
    }
}
