using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Xrv.Core;
using Xrv.Core.Menu;

namespace XrvSamples.Scenes
{
    public class ModuleScene : BaseScene
    {
        private XrvService xrv;

        public override void RegisterManagers()
        {
            base.RegisterManagers();
            Managers.AddManager(new Evergine.Bullet.BulletPhysicManager3D());
        }

        protected override void OnPostCreateXRScene()
        {
            base.OnPostCreateXRScene();

            xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);
        }

        public class TestModule1 : Xrv.Core.Modules.Module
        {
            private Entity teapot;
            private HandMenuButtonDefinition definition;

            public TestModule1()
            {
                this.definition = new HandMenuButtonDefinition()
                {
                    IconOff = EvergineContent.Materials.Icons.cross,
                    IconOn = EvergineContent.Materials.Icons.tick,
                    IsToggle = true,
                    TextOn = "Show",
                    TextOff = "Hide"
                };
            }

            public override string Name => "Test";

            public override string Description => "Description";

            public override HandMenuButtonDefinition HandMenuButton => this.definition;

            public override void Initialize(Scene scene)
            {
                var assetsService = Application.Current.Container.Resolve<AssetsService>();
                var defaultMaterial = assetsService.Load<Material>(EvergineContent.Materials.DefaultMaterial);

                this.teapot = new Entity() { IsEnabled = false }
                    .AddComponent(new Transform3D())
                    .AddComponent(new MaterialComponent() { Material = defaultMaterial })
                    .AddComponent(new TeapotMesh())
                    .AddComponent(new MeshRenderer());

                scene.Managers.EntityManager.Add(teapot);
                this.Run(true);
            }

            public override void Run(bool turnOff)
            {
                this.teapot.IsEnabled = !turnOff;
            }
        }
    }
}