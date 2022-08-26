// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
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
            var piggyBot = this.assetsService.Load<Model>(LoadModelResourceIDs.Models.PiggyBot_glb);
            this.modelEntity = piggyBot.InstantiateModelHierarchy(this.assetsService);
            this.modelEntity.FindComponent<Transform3D>().Scale = Vector3.One * 0.04f;
            this.modelEntity.IsEnabled = false;

            scene.Managers.EntityManager.Add(this.modelEntity);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.modelEntity.IsEnabled = turnOn;
        }
    }
}
