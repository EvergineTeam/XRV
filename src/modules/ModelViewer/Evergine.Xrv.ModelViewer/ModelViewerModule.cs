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
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.Lists;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Runtimes.GLB;
using Evergine.Runtimes.STL;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.ModelViewer.Effects;
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
        private Entity loadButton;

        private FileItem selectedModel;

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
        public override ButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <summary>
        /// Gets or sets the model repository list.
        /// </summary>
        public IEnumerable<Repository> Repositories { get; set; }

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
        public Func<MaterialData, Task<Material>> MaterialAssigner { get; set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.localization = this.xrv.Localization;
            this.scene = scene;

            this.HandMenuButton = new ButtonDescription()
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
            this.repositoriesListView.Columns =
            [
                new ColumnDefinition { Title = "Name", PercentageSize = 0.7f },
                new ColumnDefinition { Title = "Models", PercentageSize = 0.3f },
            ];

            this.repositoriesListView.SelectedItemChanged += (s, e) => { this.RefreshModelList(); };

            // Models list view
            this.modelsListView = repositoryWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_models", true, true);
            this.modelsListView.Columns =
            [
                new ColumnDefinition { Title = "Name", PercentageSize = 0.7f },
                new ColumnDefinition { Title = "Last update", PercentageSize = 0.3f },
            ];
            this.modelsListView.SelectedItemChanged += (s, e) => this.UpdateSelectedModel();

            // Buttons
            var loadButton = this.CreateButton(
                () => this.localization.GetString(() => Resources.Strings.Window_Load),
                this.LoadModel,
                false);
            var loadHolder = repositoryWindowEntity.FindChildrenByTag("PART_loadbutton", true, true).First();
            loadHolder.AddChild(loadButton);
            this.loadButton = loadButton;

            var cancelButton = this.CreateButton(
                () => this.localization.GetString(() => Core.Resources.Strings.Global_Cancel),
                () => { this.window.Close(); },
                true);
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

                if (this.repositoriesListView.SelectedItem is Tuple<Repository, int> selectedRepo &&
                    this.selectedModel != null)
                {
                    var extension = Path.GetExtension(this.selectedModel.Path);
                    if (this.loaders.TryGetValue(extension, out var loaderRuntime))
                    {
                        using (var stream = await selectedRepo.Item1.FileAccess.GetFileAsync(this.selectedModel.Path))
                        {
                            var materialAssignerFunc = this.MaterialAssigner == null ? this.MaterialAssignerToSolidEffect : this.MaterialAssigner;
                            model = await loaderRuntime.Read(stream, materialAssignerFunc);
                        }

                        // Instantiate model
                        var modelEntity = model.InstantiateModelHierarchy(this.assetsService);

                        // Root Entity
                        root = new Entity()
                        {
                            Tag = Path.GetFileName(this.selectedModel.Path),
                        }
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

        private Entity CreateButton(Func<string> buttonText, Action releasedAction, bool enabled)
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
            button.AddComponent(new VisuallyEnabledController
            {
                IsVisuallyEnabled = enabled,
            });
            button.FindComponentInChildren<PressableButton>().ButtonReleased += (s, e) => releasedAction.Invoke();

            return button;
        }

        private async Task ConnectToRepositoriesAsync()
        {
            this.repositoriesListView.IsEnabled = true;
            this.repositoriesListView.ShowLoadingIndicator = true;

            var repositoriesData = new List<Tuple<Repository, int>>();

            if (this.Repositories != null)
            {
                foreach (var repo in this.Repositories)
                {
                    var models = await repo.FileAccess.EnumerateFilesAsync();
                    repositoriesData.Add(Tuple.Create(repo, models.Count()));
                }

                this.repositoriesListView.Refresh();
            }

            this.repositoriesListView.DataSource = new ModelRepositoriesAdapter(repositoriesData.ToArray());
            this.repositoriesListView.ShowLoadingIndicator = false;
            this.repositoriesListView.SelectedIndex = 0;
        }

        private async void RefreshModelList()
        {
            this.modelsListView.ShowLoadingIndicator = true;

            if (this.repositoriesListView.SelectedItem is Tuple<Repository, int> repoSelected)
            {
                var models = await repoSelected.Item1.FileAccess.EnumerateFilesAsync();
                this.modelsListView.DataSource = new ModelsAdapter(models.ToList());
                this.modelsListView.Refresh();
            }

            this.modelsListView.ShowLoadingIndicator = false;
        }

        private void UpdateSelectedModel()
        {
            if (this.modelsListView.SelectedItem is FileItem selected)
            {
                // Enable button if first time model selected
                if (this.selectedModel == null)
                {
                    this.loadButton.FindComponent<VisuallyEnabledController>().IsVisuallyEnabled = true;
                }

                this.selectedModel = selected;
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

        private async Task<Material> MaterialAssignerToSolidEffect(MaterialData data)
        {
            if (this.alphaLayer == null || this.opaqueLayer == null)
            {
                this.opaqueLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                this.alphaLayer = this.assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
            }

            RenderLayerDescription layer;
            switch (data.AlphaMode)
            {
                case AlphaMode.Blend:
                    layer = data.BaseColor.A < 1.0f ? this.alphaLayer : this.opaqueLayer;
                    break;
                default:
                    layer = this.opaqueLayer;
                    break;
            }

            var effect = this.assetsService.Load<Effect>(ModelViewerResourceIDs.Effects.SolidEffect);
            var baseColorTexAndSampler = await data.GetBaseColorTextureAndSampler();

            SolidEffect material = new SolidEffect(effect)
            {
                Parameters_Color = data.BaseColor.ToVector3(),
                Parameters_Alpha = data.BaseColor.A,
                BaseColorTexture = baseColorTexAndSampler.Texture,
                BaseColorSampler = baseColorTexAndSampler.Sampler,
                LayerDescription = layer,
                Parameters_AlphaCutOff = data.AlphaCutoff,
            };

            if (data.HasVertexColor)
            {
                material.ActiveDirectivesNames = new string[] { "VERTEXCOLOR" };
            }

            if (baseColorTexAndSampler.Texture != null && baseColorTexAndSampler.Sampler != null)
            {
                material.ActiveDirectivesNames = new string[] { "TEXTURECOLOR" };
            }

            return material.Material;
        }

        private class ModelRepositoriesAdapter : ArrayAdapter<Tuple<Repository, int>>
        {
            public ModelRepositoriesAdapter(IList<Tuple<Repository, int>> data)
                : base(data)
            {
            }

            public override CellRenderer GetRenderer(int rowIndex, int columnIndex)
            {
                var repositoryData = this.Data.ElementAt(rowIndex);
                var renderer = TextCellRenderer.Instance;
                renderer.Text = columnIndex == 0 ? repositoryData.Item1.Name : repositoryData.Item2.ToString();

                return renderer;
            }
        }

        private class ModelsAdapter : ArrayAdapter<FileItem>
        {
            public ModelsAdapter(IList<FileItem> data)
                : base(data)
            {
            }

            public override CellRenderer GetRenderer(int rowIndex, int columnIndex)
            {
                var fileItem = this.Data.ElementAt(rowIndex);
                var renderer = TextCellRenderer.Instance;
                renderer.Text = columnIndex == 0 ? fileItem.Name : fileItem.ModificationTime?.ToString("dd-MM-yyyy") ?? "-";

                return renderer;
            }
        }
    }
}
