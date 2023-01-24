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
using Xrv.Core;
using Xrv.Core.Menu;
using Xrv.Core.Modules;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;
using Xrv.StreamingViewer.Components;

namespace Xrv.StreamingViewer
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class StreamingViewerModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private Scene scene;
        private ListView streamsListView;
        private Window listWindow;

        private Dictionary<string, Window> players = new Dictionary<string, Window>();

        private float playerDistance = 0.8f;
        private string playerDistanceTag = "playerDistanceTag";

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public IEnumerable<string> SourceURLs { get; set; }

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
        public string SelectedUrl => this.streamsListView.Selected.FirstOrDefault().ToString();

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.scene = scene;

            this.HandMenuButton = new MenuButtonDescription()
            {
                IconOn = StreamingViewerResourceIDs.Materials.Icons.StreamingViewer,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            this.xrv.WindowSystem.Distances.SetDistance(this.playerDistanceTag, this.playerDistance);

            var streamsWindowEntity = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingListWindow_weprefab).Instantiate();
            this.streamsListView = streamsWindowEntity.FindComponentInChildren<ListView>(true, tag: "PART_streams", true, true);
            this.streamsListView.DataSource = new ListViewData(1);
            this.streamsListView.Render = new ListViewRender()
                                .AddColumn("Feeds", 1f, TextCellRenderer.Instance);

            foreach (var source in this.SourceURLs)
            {
                this.streamsListView.DataSource.Add(source);
            }

            var size = new Vector2(0.15f, 0.18f);

            this.listWindow = this.xrv.WindowSystem.CreateWindow((config) =>
            {
                config.LocalizedTitle = () => Resources.Strings.Window_Title;
                config.Size = size;
                config.FrontPlateSize = size;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = streamsWindowEntity;
            });

            // Buttons
            var loadButton = this.CreateButton("Load", () =>
            {
                var player = this.GetOrCreatePlayer(this.SelectedUrl);
                player.Open();
            });

            var loadHolder = streamsWindowEntity.FindChildrenByTag("PART_loadbutton", true, true).First();
            loadHolder.AddChild(loadButton);

            var cancelButton = this.CreateButton("Clear", () =>
            {
                foreach (var item in this.players.Values)
                {
                    item.Close();
                }
            });

            var cancelHolder = streamsWindowEntity.FindChildrenByTag("PART_clearbutton", true, true).First();
            cancelHolder.AddChild(cancelButton);
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.listWindow.Open();
        }

        /// <summary>
        /// Gets or Create stream player.
        /// </summary>
        /// <param name="streamUrl">stream url.</param>
        /// <returns>Window asociated to that stream.</returns>
        public Window GetOrCreatePlayer(string streamUrl)
        {
            if (!this.players.TryGetValue(streamUrl, out var w))
            {
                var streamingViewerPrefab = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingViewer_weprefab).Instantiate();
                var streamingViewerComponent = streamingViewerPrefab.FindComponent<StreamingViewerComponent>();
                streamingViewerComponent.SourceURL = streamUrl;

                var size = new Vector2(0.30f, 0.30f);
                w = this.xrv.WindowSystem.CreateWindow((config) =>
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
    }
}
