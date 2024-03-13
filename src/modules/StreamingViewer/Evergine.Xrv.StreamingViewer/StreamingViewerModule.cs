// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;
using Evergine.Xrv.StreamingViewer.Components;

namespace Evergine.Xrv.StreamingViewer
{
    /// <summary>
    /// Module that shows a image gallery and lets you navigate between the different images.
    /// </summary>
    public class StreamingViewerModule : Module
    {
        private AssetsService assetsService;
        private XrvService xrv;
        private Window window = null;

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public string SourceURL { get; set; }

        /// <inheritdoc/>
        public override string Name => "Streaming Viewer";

        /// <inheritdoc/>
        public override ButtonDescription HandMenuButton { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Help { get; protected set; }

        /// <inheritdoc/>
        public override TabItem Settings { get; protected set; }

        /// <inheritdoc/>
        public override IEnumerable<string> VoiceCommands => null;

        /// <inheritdoc/>
        public override void Initialize(Scene scene)
        {
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.xrv = Application.Current.Container.Resolve<XrvService>();

            this.HandMenuButton = new ButtonDescription()
            {
                IconOn = StreamingViewerResourceIDs.Materials.Icons.StreamingViewer,
                IsToggle = false,
                TextOn = () => this.xrv.Localization.GetString(() => Resources.Strings.Menu),
            };

            var streamingViewerPrefab = this.assetsService.Load<Prefab>(StreamingViewerResourceIDs.Prefabs.StreamingViewer_weprefab).Instantiate();
            var streamingViewerComponent = streamingViewerPrefab.FindComponent<StreamingViewerComponent>();
            streamingViewerComponent.SourceURL = this.SourceURL;

            this.window = this.xrv.WindowsSystem.CreateWindow(config =>
            {
                // Initial size. Will be updated on stream load
                var size = new Vector2(0.30f, 0.30f);
                config.LocalizedTitle = () => Resources.Strings.Window_Title;
                config.Size = size;
                config.DisplayFrontPlate = false;
                config.FrontPlateSize = size;
                config.FrontPlateOffsets = Vector2.Zero;
                config.DisplayLogo = false;
                config.Content = streamingViewerPrefab;
            });
        }

        /// <inheritdoc/>
        public override void Run(bool turnOn)
        {
            this.window.Open();
        }
    }
}
