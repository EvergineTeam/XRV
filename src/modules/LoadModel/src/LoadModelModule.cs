// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
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
        /// <inheritdoc/>
        public override string Name => "LoadModel";

        /// <inheritdoc/>
        public override HandMenuButtonDescription HandMenuButton => this.handMenuDesc;

        /// <inheritdoc/>
        public override TabItem Help => null;

        /// <inheritdoc/>
        public override TabItem Settings => null;

        private AssetsService assetsService;
        private HandMenuButtonDescription handMenuDesc;
        private Entity modelEntity;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadModelModule"/> class.
        /// </summary>
        public LoadModelModule()
        {
            this.handMenuDesc = new HandMenuButtonDescription()
            {
                IsToggle = false,
                IconOn = LoadModelResourceIDs.Materials.Icons.addModel,
                TextOn = "Add Model",
            };
        }

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            // PiggyBot
            ////var piggyBot = this.assetsService.Load<Model>(LoadModelResourceIDs.Models.PiggyBot_glb);
            ////this.modelEntity = piggyBot.InstantiateModelHierarchy(this.assetsService);
            ////this.modelEntity.FindComponent<Transform3D>().Scale = Vector3.One * 0.04f;

            var material = this.assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);

            this.modelEntity = new Entity()
                            .AddComponent(new Transform3D())
                            .AddComponent(new MaterialComponent() { Material = material })
                            .AddComponent(new TeapotMesh())
                            .AddComponent(new MeshRenderer());

            // Manipulators
            this.modelEntity.AddComponent(new BoxCollider3D());
            this.modelEntity.AddComponent(new StaticBody3D() { IsSensor = true });
            this.modelEntity.AddComponent(new Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.BoundingBox());

            scene.Managers.EntityManager.Add(this.modelEntity);
            this.modelEntity.IsEnabled = false;
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.modelEntity.IsEnabled = turnOn;
        }
    }
}
