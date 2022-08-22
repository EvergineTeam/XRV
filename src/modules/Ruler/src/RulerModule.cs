using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;

namespace Xrv.Ruler
{
    public class RulerModule : Module
    {       
        public override string Name => "Ruler";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        public override TabItem Help => this.help;

        public override TabItem Settings => this.settings;

        protected AssetsService assetsService;
        private HandMenuButtonDescription handMenuDesc;
        private TabItem settings;
        private TabItem help;

        private Entity rulerEntity;
        private RulerBehavior rulerBehavior;
        private Entity rulerHelp;

        public RulerModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
            {
                IconOff = RulerResourceIDs.Materials.Icons.Measure,
                IconOn = RulerResourceIDs.Materials.Icons.Measure,
                IsToggle = true,
                TextOn = "Hide",
                TextOff = "Show"
            };

            this.settings = new TabItem()
            {
                Name = "Ruler",
                Contents = SettingContent,
            };

            this.help = new TabItem()
            {
                Name = "Ruler",
                Contents = HelpContent,
            };
        }

        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            var rulerPrefab = assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.Ruler_weprefab);

            this.rulerEntity = rulerPrefab.Instantiate();
            this.rulerEntity.IsEnabled = false;
            scene.Managers.EntityManager.Add(this.rulerEntity);

            this.rulerBehavior = this.rulerEntity.FindComponent<RulerBehavior>();
        }

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
            var material = assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);

            Entity entity = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = material })
                .AddComponent(new CubeMesh() { Size = 0.02f })
                .AddComponent(new MeshRenderer());

            return entity;
        }

        private Entity HelpContent()
        {
            if (this.rulerHelp == null)
            {
                var rulerHelpPrefab = assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.RulerHelp_weprefab);
                this.rulerHelp = rulerHelpPrefab.Instantiate();
            }

            return this.rulerHelp;
        }
    }
}
