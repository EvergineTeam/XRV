﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;

namespace Xrv.Ruler
{
    /// <summary>
    /// Ruler module implementation.
    /// </summary>
    public class RulerModule : Module
    {
        private AssetsService assetsService;
        private MenuButtonDescription handMenuDesc;
        private TabItem settings;
        private TabItem help;

        private Entity rulerEntity;
        private RulerBehavior rulerBehavior;
        private Entity rulerHelp;
        private Entity rulerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="RulerModule"/> class.
        /// </summary>
        public RulerModule()
        {
            this.handMenuDesc = new MenuButtonDescription()
            {
                IconOff = RulerResourceIDs.Materials.Icons.Measure,
                IconOn = RulerResourceIDs.Materials.Icons.Measure,
                IsToggle = true,
                TextOn = "Hide",
                TextOff = "Show",
            };

            this.settings = new TabItem()
            {
                Name = "Ruler",
                Contents = this.SettingContent,
            };

            this.help = new TabItem()
            {
                Name = "Ruler",
                Contents = this.HelpContent,
            };
        }

        /// <inheritdoc/>
        public override string Name => "Ruler";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => this.settings;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            // Ruler
            var rulerPrefab = this.assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.Ruler_weprefab);
            this.rulerEntity = rulerPrefab.Instantiate();
            this.rulerEntity.IsEnabled = false;
            scene.Managers.EntityManager.Add(this.rulerEntity);

            // Settings
            var rulerSettingPrefab = this.assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.RulerSettings_weprefab);
            this.rulerSettings = rulerSettingPrefab.Instantiate();

            // RulerBehavior
            this.rulerBehavior = this.rulerEntity.FindComponent<RulerBehavior>();
            this.rulerBehavior.Settings = this.rulerSettings;
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            if (turnOn)
            {
                this.rulerBehavior.Reset();
            }

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
    }
}
