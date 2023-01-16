// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using System.Collections.Generic;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

namespace Xrv.Painter
{
    /// <summary>
    /// Painter module, enables to paint lines in scene.
    /// </summary>
    public class PainterModule : Module
    {
        private Entity painterHelp;
        private AssetsService assetsService;
        private XrvService xrv;
        private Window painterWindow;

        /// <inheritdoc/>
        public override string Name => "Painter";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            // Menu and help entries
            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOff = PainterResourceIDs.Materials.Icons.Painter,
                IconOn = PainterResourceIDs.Materials.Icons.Painter,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.Help = new TabItem()
            {
                Name = () => this.xrv.Localization.GetString(() => Resources.Strings.Help_Tab_Name),
                Contents = this.HelpContent,
            };

            // Painter
            var painterSize = new Vector2(0.214f, 0.173f);
            this.painterWindow = this.xrv.WindowSystem.CreateWindow(async (config) =>
            {
                config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
                config.Size = painterSize;
                config.FrontPlateSize = painterSize;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;

                await EvergineBackgroundTask.Run(() =>
                {
                    var content = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.Painter).Instantiate();
                    config.Content = content;
                });
            });

            this.painterWindow.DistanceKey = Distances.NearKey;
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.painterWindow.Open();
        }

        private Entity HelpContent()
        {
            if (this.painterHelp == null)
            {
                var painterHelpPrefab = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.HelpPainer);
                this.painterHelp = painterHelpPrefab.Instantiate();
            }

            return this.painterHelp;
        }
    }
}
