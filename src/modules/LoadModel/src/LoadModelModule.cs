// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Scrolls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Xrv.LoadModel.Importers.GLB;
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

        /// <summary>
        /// Gets or sets a value indicating whether the model will be normalized to standard size or
        /// will be load using its original size.
        /// </summary>
        public bool NormalizedModelEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the box dimension of the model after normalize them. (Required NormalizedModelEnable=true).
        /// </summary>
        public float NormalizedModelSize { get; set; } = 0.2f;

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override async void Initialize(Scene scene)
        {
            // Setting cache
            foreach(var repo in this.Repositories)
            {
                if (repo.FileAccess.IsCachingEnabled)
                {
                    await repo.FileAccess.Cache.InitializeAsync();
                }
            }

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

            Entity modelEntity = null;

            await EvergineBackgroundTask.Run(async () =>
            {
                // Read glb stream
                Model model = null;
                
                var selectedRepo = this.repositoriesListView.Selected;
                if (selectedRepo != null)
                {
                    var repoName = selectedRepo[0];
                    var repo = this.Repositories.FirstOrDefault(r => r.Name == repoName);
                    var modelSelected = this.modelsListView.Selected;

                    if (modelSelected != null)
                    {
                        var filePath = modelSelected[0];
                        using (var stream = await repo.FileAccess.GetFileAsync(filePath))
                        using (var memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            model = GLBRuntime.Instance.Read(memoryStream);
                        }

                        // Instantiate model
                        modelEntity = model.InstantiateModelHierarchy(this.assetsService);

                        // Normalizing size
                        if (this.NormalizedModelEnabled)
                        {
                            modelEntity.FindComponent<Transform3D>().Scale = Vector3.One * (this.NormalizedModelSize / model.BoundingBox.Value.HalfExtent.Length());
                        }

                        // Add additional components
                        BoundingBox boundingBox = model.BoundingBox.HasValue ? model.BoundingBox.Value : default;
                        this.AddManipulatorComponents(modelEntity, boundingBox);
                    }
                }
            });

            if (modelEntity != null)
            {
                loadModelBehavior.ModelEntity = modelEntity;
            }
            else
            {
                this.scene.Managers.EntityManager.Remove(manipulatorEntity);
            }
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

            var repoSelected = this.repositoriesListView.Selected;
            if (repoSelected != null)
            {
                var repoName = repoSelected[0];
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

        private void AddManipulatorComponents(Entity entity, BoundingBox boundingBox)
        {
            // Add global bounding box
            entity.AddComponent(new BoxCollider3D()
            {
                Size = boundingBox.HalfExtent * 2,
                Offset = boundingBox.Center,
            });
            entity.AddComponent(new StaticBody3D());
            entity.AddComponent(new Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.BoundingBox()
            {
                AutoCalculate = false,
                ScaleHandleScale = 0.030f,
                RotationHandleScale = 0.030f,
                LinkScale = 0.001f,
                BoxPadding = Vector3.One * 0.1f,
                BoxMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxVisual),
                BoxGrabbedMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxVisualGrabbed),
                ShowWireframe = true,
                ShowScaleHandles = true,
                ShowXScaleHandle = true,
                ShowYScaleHandle = true,
                ShowZScaleHandle = true,
                ShowXRotationHandle = true,
                ShowYRotationHandle = true,
                ShowZRotationHandle = true,
                WireframeShape = Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.WireframeType.Cubic,
                WireframeMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxWireframe),
                HandleMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxHandleBlue),
                HandleGrabbedMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxHandleBlueGrabbed),
                ScaleHandlePrefab = this.assetsService.Load<Prefab>(MRTKResourceIDs.Prefabs.BoundingBox_ScaleHandle_weprefab),
                RotationHandlePrefab = this.assetsService.Load<Prefab>(MRTKResourceIDs.Prefabs.BoundingBox_RotateHandle_weprefab),
                FaceScaleHandlePrefab = this.assetsService.Load<Prefab>(MRTKResourceIDs.Prefabs.BoundingBox_FaceScaleHandle_weprefab),
                HandleFocusedMaterial = this.assetsService.Load<Material>(MRTKResourceIDs.Materials.BoundingBox.BoundingBoxHandleBlueFocused),
            });
            ////entity.AddComponent(new MinScaleConstraint() { MinimumScale = Vector3.One * 0.1f });
            entity.AddComponent(new SimpleManipulationHandler()
            {
                SmoothingActive = true,
                SmoothingAmount = 0.001f,
                EnableSinglePointerRotation = true,
                KeepRigidBodyActiveDuringDrag = false,
                IncludeChildrenColliders = true,
            });
        }
    }
}
