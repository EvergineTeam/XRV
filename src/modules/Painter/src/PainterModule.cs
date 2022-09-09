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
using Xrv.Painter.Components;

namespace Xrv.Painter
{
    /// <summary>
    /// Painter module, enables to paint lines in scene.
    /// </summary>
    public class PainterModule : Module
    {
        private MenuButtonDescription handMenuDesc;
        private TabItem help;
        private Entity painterHelp;
        private AssetsService assetsService;
        private XrvService xrv;
        private Window painterWindow;
        private IEnumerable<string> voiceCommands;

        /// <summary>
        /// Initializes a new instance of the <see cref="PainterModule"/> class.
        /// </summary>
        public PainterModule()
        {
            this.handMenuDesc = new MenuButtonDescription()
            {
                IconOff = PainterResourceIDs.Materials.Icons.Painter,
                IconOn = PainterResourceIDs.Materials.Icons.Painter,
                IsToggle = true,
                TextOn = "Hide",
                TextOff = "Show",
            };

            this.help = new TabItem()
            {
                Name = "Painter",
                Contents = this.HelpContent,
            };

            this.voiceCommands = new List<string>() { "paint", "hand", "erase", "thin", "thick", "medium" };
        }

        /// <inheritdoc/>
        public override string Name => "Painter";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => null;

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => this.voiceCommands;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            // Painter
            var painterSize = new Vector2(0.175f, 0.15f);
            this.painterWindow = this.xrv.WindowSystem.CreateWindow(async (config) =>
            {
                config.Title = "Paint";
                config.Size = painterSize;
                config.FrontPlateSize = painterSize;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;

                await EvergineBackgroundTask.Run(async () =>
                {
                    var content = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.Painter).Instantiate();
                    var cursor = content.FindComponent<PainterCursor>();
                    cursor.Pointer = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.PointerPainter).Instantiate();
                    config.Content = content;
                    await EvergineForegroundTask.Run(() =>
                    {
                        scene.Managers.EntityManager.Add(cursor.Pointer);
                        cursor.Pointer.IsEnabled = false;
                    });
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
