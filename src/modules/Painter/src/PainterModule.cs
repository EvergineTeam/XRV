// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;

namespace Xrv.Painter
{
    /// <summary>
    /// Painter module, enables to paint lines in scene.
    /// </summary>
    public class PainterModule : Module
    {
        private HandMenuButtonDescription handMenuDesc;
        private TabItem help;
        private Entity painterEntity;
        private Entity painterHelp;
        private AssetsService assetsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PainterModule"/> class.
        /// </summary>
        public PainterModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
            {
                IconOff = PainterResourceIDs.Materials.Icons.Paint,
                IconOn = PainterResourceIDs.Materials.Icons.Paint,
                IsToggle = true,
                TextOn = "Hide",
                TextOff = "Show",
            };

            this.help = new TabItem()
            {
                Name = "Painter",
                Contents = this.HelpContent,
            };
        }

        /// <inheritdoc/>
        public override string Name => "Painter";

        /// <inheritdoc/>
        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => this.help;

        /// <inheritdoc/>
        public override TabItem Settings => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            // Painter
            var painterPrefab = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.Painter_weprefab);
            this.painterEntity = painterPrefab.Instantiate();
            this.painterEntity.IsEnabled = false;
            scene.Managers.EntityManager.Add(this.painterEntity);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.painterEntity.IsEnabled = turnOn;
        }

        private Entity HelpContent()
        {
            if (this.painterHelp == null)
            {
                var painterHelpPrefab = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.PainterHelp_weprefab);
                this.painterHelp = painterHelpPrefab.Instantiate();
            }

            return this.painterHelp;
        }
    }
}
