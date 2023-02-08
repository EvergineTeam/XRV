// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.Lists;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Menu;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.ModelViewer.Effects;
using Evergine.Xrv.ModelViewer.Importers;
using Evergine.Xrv.ModelViewer.Importers.GLB;
using Evergine.Xrv.ModelViewer.Importers.STL;
using Evergine.Xrv.ModelViewer.Structs;
using static glTFLoader.Schema.Material;
using Window = Evergine.Xrv.Core.UI.Windows.Window;

namespace Evergine.Xrv.ModelViewer
{
    /// <summary>
    /// This module allows load 3D models.
    /// </summary>
    public class ModelViewerModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private LocalizationService localization;
        private Scene scene;
        private Prefab manipulatorPrefab;
        private Prefab repositoryWindow;

        private ListView repositoriesListView;
        private ListView modelsListView;
        private Entity repositoriesLoading;
        private Entity modelsLoading;

        private RenderLayerDescription opaqueLayer;
        private RenderLayerDescription alphaLayer;

        private Window window = null;
        private Dictionary<string, ModelRuntime> loaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelViewerModule"/> class.
        /// </summary>
        public ModelViewerModule()
        {
            // 3D format supported.
            this.loaders = new Dictionary<string, ModelRuntime>
            {
                { GLBRuntime.Instance.Extentsion, GLBRuntime.Instance },
                { STLRuntime.Instance.Extentsion, STLRuntime.Instance },
            };
        }

        /// <inheritdoc/>
        public override string Name => "LoadModel";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <summary>
        /// Gets or sets the model repository list.
        /// </summary>
        public Repository[] Repositories { get; set; }

        /// <summary>
        /// Gets or sets the DataSource collection.
        /// Ej(extension, ModelRuntime instance).
        ///    { GLBRuntime.Instance.Extentsion, GLBRuntime.Instance }.
        ///    { STLRuntime.Instance.Extentsion, STLRuntime.Instance }.
        /// </summary>
        public Dictionary<string, ModelRuntime> Loaders
        {
            get => this.loaders;
            set => this.loaders = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the model will be normalized to standard size or
        /// will be load using its original size.
        /// </summary>
        public bool NormalizedModelEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the box dimension of the model after normalize them. (Required NormalizedModelEnable=true).
        /// </summary>
        public float NormalizedModelSize { get; set; } = 0.2f;

        /// <summary>
        /// Gets or sets the material created by the loader.
        /// </summary>
        public Func<Color, Texture, SamplerState, AlphaModeEnum, float, float, bool, Material> MaterialAssigner { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.localization = this.xrv.Localization;
            this.scene = scene;

            this.HandMenuButton = new MenuButtonDescription()
            {
                IsToggle = false,
                IconOn = ModelViewerResourceIDs.Materials.Icons.addModel,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.manipulatorPrefab = this.assetsService.Load<Prefab>(ModelViewerResourceIDs.Prefabs.Manipulator_weprefab);
            this.repositoryWindow = this.assetsService.Load<Prefab>(ModelViewerResourceIDs.Prefabs.RepositoriesWindow_weprefab);
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
            var loadButton = this.CreateButton(
                () => this.localization.GetString(() => Resources.Strings.Window_Load),
                this.LoadModel);
            var loadHolder = repositoryWindowEntity.FindChildrenByTag("PART_loadbutton", true, true).First();
            loadHolder.AddChild(loadButton);

            var cancelButton = this.CreateButton(
                () => this.localization.GetString(() => Core.Resources.Strings.Global_Cancel),
                () => { this.window.Close(); });
            var cancelHolder = repositoryWindowEntity.FindChildrenByTag("PART_cancelbutton", true, true).First();
            cancelHolder.AddChild(cancelButton);

            this.window = this.xrv.WindowsSystem.CreateWindow((config) =>
            {
                config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
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

            Entity root = null;

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
                        var extension = Path.GetExtension(filePath);

                        if (this.loaders.TryGetValue(extension, out var loaderRuntime))
                        {
                            using (var stream = await repo.FileAccess.GetFileAsync(filePath))
                            using (var memoryStream = new MemoryStream())
                            {
                                stream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                var materialAssignerFunc = this.MaterialAssigner == null ? this.MaterialAssignerToSolidEffect : this.MaterialAssigner;
                                model = await loaderRuntime.Read(memoryStream, materialAssignerFunc);
                            }

                            // Instantiate model
                            var modelEntity = model.InstantiateModelHierarchy(this.assetsService);

                            // Root Entity
                            root = new Entity()
                                .AddComponent(new Transform3D());

                            root.AddChild(modelEntity);

                            // BoundingBox
                            BoundingBox boundingBox = model.BoundingBox.HasValue ? model.BoundingBox.Value : default;
                            boundingBox.Transform(modelEntity.FindComponent<Transform3D>().WorldTransform);

                            // Normalizing size
                            if (this.NormalizedModelEnabled)
                            {
                                root.FindComponent<Transform3D>().Scale = Vector3.One * (this.NormalizedModelSize / boundingBox.HalfExtent.Length());
                            }

                            // Add additional components
                            this.AddManipulatorComponents(root, boundingBox);
                        }
                        else
                        {
                            throw new NotSupportedException($"3D format {extension} not supported.");
                        }
                    }
                }
            });

            if (root != null)
            {
                loadModelBehavior.ModelEntity = root;
            }
            else
            {
                this.scene.Managers.EntityManager.Remove(manipulatorEntity);
            }
        }

        private Entity CreateButton(Func<string> buttonText, Action releasedAction)
        {
            var localization = this.xrv.Localization;
            var buttonPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
            var button = buttonPrefab.Instantiate();
            button.AddComponent(new StandardButtonConfigurator
            {
                Plate = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.PrimaryColor1),
            });
            button.AddComponent(new ButtonLocalization
            {
                LocalizationFunc = buttonText,
            });
            button.FindComponentInChildren<PressableButton>().ButtonReleased += (s, e) => releasedAction.Invoke();

            return button;
        }

        private async Task ConnectToRepositoriesAsync()
        {
            this.repositoriesListView.ClearData();
            var repositoriesDataSource = this.repositoriesListView.DataSource;
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
            this.modelsListView.ClearData();
            this.modelsLoading.IsEnabled = true;

            var repoSelected = this.repositoriesListView.Selected;
            if (repoSelected != null)
            {
                var repoName = repoSelected[0];
                var repo = this.Repositories.FirstOrDefault(r => r.Name == repoName);
                var models = await repo.FileAccess.EnumerateFilesAsync();

                var modelsDataSource = this.modelsListView.DataSource;
                foreach (var modelFile in models)
                {
                    modelsDataSource.Add(modelFile.Name, modelFile.ModificationTime.Value.ToString("dd-MM-yyyy"));
                }

                this.modelsListView.Refresh();

                this.modelsLoading.IsEnabled = false;
            }
        }

        private void AddManipulatorComponents(Entity root, BoundingBox boundingBox)
        {
            // Add global bounding box
            root.AddComponent(new BoxCollider3D()
            {
                Size = boundingBox.HalfExtent * 2,
                Offset = boundingBox.Center,
            });
            root.AddComponent(new StaticBody3D());
            root.AddComponent(new Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.BoundingBox()
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
            root.AddComponent(new SimpleManipulationHandler()
            {
                SmoothingActive = true,
                SmoothingAmount = 0.001f,
                EnableSinglePointerRotation = true,
                KeepRigidBodyActiveDuringDrag = false,
                IncludeChildrenColliders = true,
            });
        }

        private Material MaterialAssignerToSolidEffect(Color baseColor, Texture baseColorTexture, SamplerState baseColorSampler, AlphaModeEnum alphaMode, float alpha, float alphaCutOff, bool vertexColorEnabled)
        {
            if (this.alphaLayer == null || this.opaqueLayer == null)
            {
                this.opaqueLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                this.alphaLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
            }

            RenderLayerDescription layer;
            switch (alphaMode)
            {
                default:
                case AlphaModeEnum.MASK:
                case AlphaModeEnum.OPAQUE:
                    layer = this.opaqueLayer;
                    break;
                case AlphaModeEnum.BLEND:
                    layer = alpha < 1.0f ? this.alphaLayer : this.opaqueLayer;
                    break;
            }

            var effect = this.assetsService.Load<Effect>(ModelViewerResourceIDs.Effects.SolidEffect);
            SolidEffect material = new SolidEffect(effect)
            {
                Parameters_Color = baseColor.ToVector3(),
                Parameters_Alpha = alpha,
                BaseColorTexture = baseColorTexture,
                BaseColorSampler = baseColorSampler,
                LayerDescription = layer,
                Parameters_AlphaCutOff = alphaCutOff,
            };

            if (vertexColorEnabled)
            {
                material.ActiveDirectivesNames = new string[] { "VERTEXCOLOR" };
            }

            if (baseColorTexture != null)
            {
                material.ActiveDirectivesNames = new string[] { "TEXTURECOLOR" };
            }

            return material.Material;
        }
    }
}
