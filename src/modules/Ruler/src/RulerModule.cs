using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.Settings;

namespace Xrv.Ruler
{
    public class RulerModule : Module
    {
        private HandMenuButtonDescription handMenuDesc;
        private Section settings;

        private Entity rulerEntity;

        public override string Name => "Ruler";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        public override Section Help => null;

        public override Section Settings => this.settings;

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

            this.settings = new Section()
            {
                Name = "Ruler",
                Contents = SettingContent,
            };
        }

        public override void Initialize(Scene scene)
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var rulerPrefab = assetsService.Load<Prefab>(RulerResourceIDs.Prefabs.Ruler_weprefab);

            this.rulerEntity = rulerPrefab.Instantiate();
            this.rulerEntity.IsEnabled = false;
            scene.Managers.EntityManager.Add(this.rulerEntity);            
        }

        public override void Run(bool turnOn)
        {
            this.rulerEntity.IsEnabled = turnOn;
        }

        private Entity SettingContent()
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            var material = assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);

            Entity entity = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = material })
                .AddComponent(new CubeMesh() { Size = 0.02f })
                .AddComponent(new MeshRenderer());

            return entity;
        }
    }
}
