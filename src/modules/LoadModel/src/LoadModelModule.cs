// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Scrolls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Xrv.LoadModel.Structs;

namespace Xrv.LoadModel
{
    /// <summary>
    /// This module allows load 3D models.
    /// </summary>
    public class LoadModelModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private Scene scene;
        private Prefab manipulatorPrefab;
        private Prefab repositoryWindow;
        private MenuButtonDescription handMenuDesc;

        private ListView repositoriesListView;
        private ListView modelsListView;
        private Entity repositoriesLoading;
        private Entity modelsLoading;

        private Window window = null;

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

        /// <summary>
        /// Gets or sets the model repository list.
        /// </summary>
        public Repository[] Repositories { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            this.manipulatorPrefab = this.assetsService.Load<Prefab>(LoadModelResourceIDs.Prefabs.Manipulator_weprefab);
            this.repositoryWindow = this.assetsService.Load<Prefab>(LoadModelResourceIDs.Prefabs.RepositoriesWindow_weprefab);
            var repositoryWindowEntity = this.repositoryWindow.Instantiate();

            // Repositories list view
            this.repositoriesListView = repositoryWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_repositories", true, true);
            this.repositoriesListView.DataSource = new ListViewData(2);
            this.repositoriesListView.Render = new ListViewRender()
                                .AddColumn("Name", 0.7f, TextCellRenderer.Instance)
                                .AddColumn("Models", 0.3f, TextCellRenderer.Instance);

            this.repositoriesListView.SelectedChanged += (s, e) => { this.RefreshModelList(); };
            this.repositoriesLoading = repositoryWindowEntity.FindChildrenByTag("PART_repositories_loading", true, true).First();

            // Models list view
            this.modelsListView = repositoryWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_models", true, true);
            this.modelsListView.DataSource = new ListViewData(2);
            this.modelsListView.Render = new ListViewRender()
                                .AddColumn("Name", 0.7f, TextCellRenderer.Instance)
                                .AddColumn("Last update", 0.3f, TextCellRenderer.Instance);
            this.modelsLoading = repositoryWindowEntity.FindChildrenByTag("PART_models_loading", true, true).First();

            // Buttons
            var loadButton = this.CreateButton("Load", this.LoadModel);
            var loadHolder = repositoryWindowEntity.FindChildrenByTag("PART_loadbutton", true, true).First();
            loadHolder.AddChild(loadButton);

            var cancelButton = this.CreateButton("Cancel", () => { this.window.Close(); });
            var cancelHolder = repositoryWindowEntity.FindChildrenByTag("PART_cancelbutton", true, true).First();
            cancelHolder.AddChild(cancelButton);

            this.window = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.Title = "Load model from repository";
                config.Content = repositoryWindowEntity;
                config.DisplayFrontPlate = false;
                config.Size = new Vector2(0.3f, 0.22f);
            });
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.window.Open();
            _ = this.ConnectToRepositoriesAsync();
        }

        private async void LoadModel()
        {
            this.window.Close();

            Entity manipulatorEntity = null;
            LoadModelBehavior loadModelBehavior = null;
            await EvergineBackgroundTask.Run(() =>
            {
                // Instanciate manipulator prefab
                manipulatorEntity = this.manipulatorPrefab.Instantiate();
                loadModelBehavior = manipulatorEntity.FindComponent<LoadModelBehavior>();

                // Create in front of the viewer
                var cameraTransform = this.scene.Managers.RenderManager.ActiveCamera3D.Transform;
                var cameraWorldTransform = cameraTransform.WorldTransform;
                var center = cameraTransform.Position + (cameraWorldTransform.Forward * 0.6f);
                manipulatorEntity.FindComponent<Transform3D>().Position = center;
            });

            this.scene.Managers.EntityManager.Add(manipulatorEntity);

            await EvergineBackgroundTask.Run(() =>
            {
                Thread.Sleep(2000);

                // Teapot
                var material = this.assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID);
                var modelEntity = new Entity()
                                .AddComponent(new Transform3D() { LocalScale = Vector3.One * 0.2f })
                                .AddComponent(new MaterialComponent() { Material = material })
                                .AddComponent(new TeapotMesh())
                                .AddComponent(new MeshRenderer());

                loadModelBehavior.ModelEntity = modelEntity;
            });
        }

        private Entity CreateButton(string buttonText, Action releasedAction)
        {
            var buttonPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
            var button = buttonPrefab.Instantiate();
            var buttonText3D = button.FindComponentInChildren<Text3DMesh>(true, "PART_Text", true, true);
            buttonText3D.Text = buttonText;
            var buttonPlate = button.FindComponentInChildren<MaterialComponent>(true, "PART_Plate", true, true);
            buttonPlate.Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.PrimaryColor1);
            Workarounds.MrtkRotateButton(button);

            button.FindComponentInChildren<PressableButton>().ButtonReleased += (s, e) => releasedAction.Invoke();

            return button;
        }

        private async Task ConnectToRepositoriesAsync()
        {
            var repositoriesDataSource = this.repositoriesListView.DataSource;
            repositoriesDataSource.ClearData();
            this.repositoriesLoading.IsEnabled = true;
            foreach (var repo in this.Repositories)
            {
                var name = repo.Name;
                var models = await repo.FileAccess.EnumerateFilesAsync();
                repositoriesDataSource.Add(name, models.Count().ToString());
            }

            this.repositoriesListView.Refresh();

            this.RefreshModelList();
            this.repositoriesLoading.IsEnabled = false;
        }

        private async void RefreshModelList()
        {
            var modelsDataSource = this.modelsListView.DataSource;
            modelsDataSource.ClearData();

            this.modelsLoading.IsEnabled = true;

            var repoName = this.repositoriesListView.Selected[0];
            var repo = this.Repositories.FirstOrDefault(r => r.Name == repoName);
            var models = await repo.FileAccess.EnumerateFilesAsync();

            foreach (var modelFile in models)
            {
                modelsDataSource.Add(modelFile.Name, modelFile.ModificationTime.Value.ToString("dd-MM-yyyy"));
            }

            this.modelsListView.Refresh();

            this.modelsLoading.IsEnabled = false;
        }
    }
}
