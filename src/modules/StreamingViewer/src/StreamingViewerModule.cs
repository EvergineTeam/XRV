// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Scrolls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Xrv.StreamingViewer.Components;
using Xrv.StreamingViewer.Structs;

namespace Xrv.StreamingViewer
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class StreamingViewerModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;

        private Dictionary<string, Window> players = new Dictionary<string, Window>();
        private Dictionary<string, string> feedDic = new Dictionary<string, string>();

        private float playerDistance = 0.8f;
        private string playerDistanceTag = "playerDistanceTag";
        private Prefab streamWindow;
        private ListView streamListView;
        private Entity streamsLoading;
        private ListView feedListView;
        private Entity feedlsLoading;
        private Window window;

        /// <summary>
        /// Gets or sets the model repository list.
        /// </summary>
        public Streams[] Streams { get; set; }

        /// <summary>
        /// Gets or sets the width of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the images listed in the gallery.
        /// </summary>
        public uint ImagePixelsHeight { get; set; }

        /// <inheritdoc/>
        public override string Name => "Streaming Viewer";

        /// <inheritdoc/>
        public override MenuButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <summary>
        /// Gets current Selected url from list.
        /// </summary>
        public string SelectedUrl => this.feedListView.Selected.FirstOrDefault().ToString();

        /// <summary>
        /// Gets current Selected stream from list.
        /// </summary>
        public string SelectedStream => this.streamListView.Selected.FirstOrDefault().ToString();

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOn = StreamingViewerResourceIDs.Materials.Icons.StreamingViewer,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.xrv.WindowsSystem.Distances.SetDistance(this.playerDistanceTag, this.playerDistance);

            this.streamWindow = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamsWindow_weprefab);
            var streamWindowEntity = this.streamWindow.Instantiate();

            // Repositories list view
            this.streamListView = streamWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_repositories", true, true);
            this.streamListView.DataSource = new ListViewData(2);
            this.streamListView.Render = new ListViewRender()
                                .AddColumn("Name", 0.7f, TextCellRenderer.Instance)
                                .AddColumn("Feeds", 0.3f, TextCellRenderer.Instance);

            this.streamListView.SelectedChanged += (s, e) => { this.RefreshModelList(); };
            this.streamsLoading = streamWindowEntity.FindChildrenByTag("PART_repositories_loading", true, true).First();

            // Models list view
            this.feedListView = streamWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_models", true, true);
            this.feedListView.DataSource = new ListViewData(1);
            this.feedListView.Render = new ListViewRender()
                                .AddColumn("Name", 1f, TextCellRenderer.Instance);

            this.feedlsLoading = streamWindowEntity.FindChildrenByTag("PART_feeds_loading", true, true).First();

            // Buttons
            var loadButton = this.CreateButton("Load", () =>
            {
                var player = this.GetOrCreatePlayer(this.SelectedStream, this.SelectedUrl);
                player.Open();
            });

            var loadHolder = streamWindowEntity.FindChildrenByTag("PART_loadbutton", true, true).First();
            loadHolder.AddChild(loadButton);

            var cancelButton = this.CreateButton("Close", () => this.window.Close());

            var cancelHolder = streamWindowEntity.FindChildrenByTag("PART_cancelbutton", true, true).First();
            cancelHolder.AddChild(cancelButton);

            this.window = this.xrv.WindowsSystem.CreateWindow((config) =>
            {
                config.LocalizedTitle = () => this.xrv.Localization.GetString(() => Resources.Strings.Window_Title);
                config.Content = streamWindowEntity;
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

        /// <summary>
        /// Gets or Create stream player.
        /// </summary>
        /// <param name="selectedRepo">selected repo.</param>
        /// <param name="selectedFeed">selected feed.</param>
        /// <returns>Window asociated to that stream.</returns>
        public Window GetOrCreatePlayer(string selectedRepo, string selectedFeed)
        {
            var key = $"{selectedRepo}{selectedFeed}";
            this.feedDic.TryGetValue(key, out var streamUrl);

            if (!this.players.TryGetValue(streamUrl, out var w))
            {
                var streamingViewerPrefab = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingViewer_weprefab).Instantiate();
                var streamingViewerComponent = streamingViewerPrefab.FindComponent<StreamingViewerComponent>();
                streamingViewerComponent.SourceURL = streamUrl;

                var size = new Vector2(0.30f, 0.30f);
                w = this.xrv.WindowsSystem.CreateWindow((config) =>
                {
                    config.LocalizedTitle = () => streamUrl;
                    config.Size = size;
                    config.FrontPlateSize = size;
                    config.FrontPlateOffsets = Vector2.Zero;
                    config.DisplayLogo = false;
                    config.Content = streamingViewerPrefab;
                });

                this.players[streamUrl] = w;
                w.DistanceKey = this.playerDistanceTag;
            }

            return w;
        }

        private async Task ConnectToRepositoriesAsync()
        {
            this.streamListView.ClearData();
            var repositoriesDataSource = this.streamListView.DataSource;
            this.streamsLoading.IsEnabled = true;
            foreach (var repo in this.Streams)
            {
                var name = repo.Name;
                var models = await repo.FileAccess.EnumerateFilesAsync();
                repositoriesDataSource.Add(name, models.Count().ToString());
            }

            this.streamListView.Refresh();

            this.RefreshModelList();
            this.streamsLoading.IsEnabled = false;
        }

        private Entity CreateButton(string buttonText, Action releasedAction)
        {
            var buttonPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
            var button = buttonPrefab.Instantiate();
            var buttonText3D = button.FindComponentInChildren<Text3DMesh>(true, "PART_Text", true, true);
            buttonText3D.Text = buttonText;
            var buttonPlate = button.FindComponentInChildren<MaterialComponent>(true, "PART_Plate", true, true);
            buttonPlate.Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.PrimaryColor1);

            button.FindComponentInChildren<PressableButton>().ButtonReleased += (s, e) => releasedAction.Invoke();

            return button;
        }

        private async void RefreshModelList()
        {
            this.feedListView.ClearData();
            this.feedlsLoading.IsEnabled = true;

            var repoSelected = this.streamListView.Selected;
            if (repoSelected != null)
            {
                var repoName = repoSelected[0];
                var repo = this.Streams.FirstOrDefault(r => r.Name == repoName);
                var models = await repo.FileAccess.EnumerateFilesAsync();

                var modelsDataSource = this.feedListView.DataSource;
                foreach (var modelFile in models)
                {
                    var stream = await repo.FileAccess.GetFileAsync(modelFile.Path);
                    var data = await JsonSerializer.DeserializeAsync<StreamSource[]>(stream);
                    foreach (var item in data)
                    {
                        modelsDataSource.Add(item.Name);
                        this.feedDic[repoName + item.Name] = item.Url;
                    }
                }

                this.feedListView.Refresh();

                this.feedlsLoading.IsEnabled = false;
            }
        }
    }
}
