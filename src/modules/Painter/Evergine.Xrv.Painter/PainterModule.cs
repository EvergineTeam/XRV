// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using System.Linq;
using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;
using Evergine.Xrv.Painter.Components;

namespace Evergine.Xrv.Painter
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
        public override ButtonDescription HandMenuButton { get; protected set; }

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
            this.HandMenuButton = new ButtonDescription()
            {
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
            this.painterWindow = this.xrv.WindowsSystem.CreateWindow(async (config) =>
            {
                config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
                config.Size = painterSize;
                config.FrontPlateSize = painterSize;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;

                await EvergineBackgroundTask.Run(async () =>
                {
                    var content = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.Painter).Instantiate();
                    var painterCursors = content.FindComponents<PainterCursor>().ToArray();

                    var pointerPainterPrefab = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.PointerPainter);

                    foreach (var painterCursor in painterCursors)
                    {
                        painterCursor.Pointer = pointerPainterPrefab.Instantiate();
                    }

                    await EvergineForegroundTask.Run(() =>
                    {
                        config.Content = content;

                        foreach (var painterCursor in painterCursors)
                        {
                            scene.Managers.EntityManager.Add(painterCursor.Pointer);
                            painterCursor.Pointer.IsEnabled = false;
                        }
                    });
                });
            });

            this.painterWindow.DistanceKey = Distances.MediumKey;
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
