// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using System.Threading.Tasks;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;

namespace Xrv.LoadModel
{
    /// <summary>
    /// This module allows load 3D models.
    /// </summary>
    public class LoadModelModule : Module
    {

        private AssetsService assetsService;
        private Scene scene;
        private MenuButtonDescription handMenuDesc;
        private Entity modelEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadModelModule"/> class.
        /// </summary>
        public LoadModelModule()
        {
            this.handMenuDesc = new MenuButtonDescription()
            {
                IsToggle = false,
                IconOn = LoadModelResourceIDs.Materials.Icons.addModel,
                TextOn = "Add Model",
            };
        }

        /// <inheritdoc/>
        public override string Name => "LoadModel";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => null;

        /// <inheritdoc/>
        public override TabItem Settings => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.scene = scene;
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            // Create manipulator prefab
            var manipulatorPrefab = this.assetsService.Load<Prefab>(LoadModelResourceIDs.Prefabs.Manipulator_weprefab);
            var manipulatorEntity = manipulatorPrefab.Instantiate();
            var loadModelBehavior = manipulatorEntity.FindComponent<LoadModelBehavior>();

            // Create in front of the viewer
            var cameraTransform = this.scene.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            var center = cameraTransform.Position + (cameraWorldTransform.Forward * 0.6f);
            manipulatorEntity.FindComponent<Transform3D>().Position = center;

            this.scene.Managers.EntityManager.Add(manipulatorEntity);

            // Load GLB model
            EvergineForegroundTask.Run(async () =>
            {
                await Task.Delay(2000);

                // Teapot
                var material = this.assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);
                var modelEntity = new Entity()
                                .AddComponent(new Transform3D() { LocalScale = Vector3.One * 0.2f })
                                .AddComponent(new MaterialComponent() { Material = material })
                                .AddComponent(new TeapotMesh())
                                .AddComponent(new MeshRenderer());

                // PiggyBot
                ////var model = this.assetsService.Load<Model>(LoadModelResourceIDs.Models.PiggyBot_glb);
                ////var modelEntity = model.InstantiateModelHierarchy(this.assetsService);
                ////var transform = modelEntity.FindComponent<Transform3D>();
                ////transform.LocalScale = Vector3.One * 0.01f;
                ////var aabb = model.BoundingBox.Value;

                loadModelBehavior.ModelEntity = modelEntity;
            });
        }
    }
}
