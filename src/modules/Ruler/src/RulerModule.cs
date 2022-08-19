using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Xrv.Core.Menu;
using Xrv.Core.Modules;

namespace Xrv.Ruler
{
    public class RulerModule : Module
    {
        private HandMenuButtonDescription handMenuDesc;
        private Entity rulerEntity;

        public override string Name => "Ruler";

        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

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
    }
}
