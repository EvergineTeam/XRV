// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.UI.Windows;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Application = Evergine.Framework.Application;
using PixelFormat = Evergine.Common.Graphics.PixelFormat;

namespace Evergine.Xrv.StreamingViewer.Components
{
    /// <summary>
    /// Module that shows a live video streaming.
    /// </summary>
    public class StreamingViewerComponent : Component
    {
        [BindService]
        private readonly XrvService xrvService = null;

        [BindService]
        private readonly GraphicsContext graphicsContext = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_video_frame")]
        private readonly MaterialComponent videoFrameMaterial = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_video_frame")]
        private readonly PlaneMesh videoFramePlaneMesh = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_video_spinner", isRecursive: true)]
        private readonly Entity spinnerEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_connection_error", isRecursive: true)]
        private readonly Entity connectionErrorTextEntity = null;

        [BindComponent]
        private readonly Transform3D transform = null;

        private WindowConfigurator windowConfigurator = null;

        private Texture imageTexture = null;
        private bool initializedTexture = false;
        private bool stop;

        private ILogger logger;
        private HttpClient httpClient;

        private const float PixelsInAMeter = 2000f;
        private const float BottomMarginForLogo = 0.05f;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingViewerComponent"/> class.
        /// </summary>
        public StreamingViewerComponent()
        {
            this.httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public string SourceURL { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.windowConfigurator = this.Owner.FindComponentInParents<WindowConfigurator>();
            if (!Application.Current.IsEditor)
            {
                this.connectionErrorTextEntity.IsEnabled = false;
                this.videoFramePlaneMesh.Owner.IsEnabled = true;
                this.stop = false;
                _ = this.TryGetVideoAsync();
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.stop = true;
        }

        private async Task TryGetVideoAsync()
        {
            try
            {
                using var responseStream = await this.httpClient.GetStreamAsync(this.SourceURL).ConfigureAwait(false);

                int responseByte;
                bool atEndOfLine = false;
                string line = string.Empty;
                int size = 0;

                // This loop goes as long as streaming is on
                while (!this.stop && (responseByte = responseStream.ReadByte()) != -1)
                {
                    // Ignore Blanks
                    if (responseByte == 10)
                    {
                        continue;
                    }

                    // Check if Carriage Return (We will start a new line)
                    if (responseByte == 13)
                    {
                        // Check if two blank lines (end of header)
                        if (atEndOfLine)
                        {
                            responseStream.ReadByte();

                            // Read all
                            await this.ReadStreamingAsync(responseStream, size).ConfigureAwait(false);
                            atEndOfLine = false;
                            line = string.Empty;
                        }
                        else
                        {
                            atEndOfLine = true;
                        }

                        if (line.ToLower().StartsWith("content-length:"))
                        {
                            size = Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                        }
                        else
                        {
                            line = string.Empty;
                        }
                    }
                    else
                    {
                        atEndOfLine = false;
                        line += (char)responseByte;
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Streaming connection error");
                this.connectionErrorTextEntity.IsEnabled = true;
                this.spinnerEntity.IsEnabled = false;
                this.videoFramePlaneMesh.Owner.IsEnabled = false;
            }
        }

        private async Task ReadStreamingAsync(Stream stream, int bytesToRead)
        {
            var shared = ArrayPool<byte>.Shared;
            var buffer = shared.Rent(bytesToRead);

            int bytesLeft = bytesToRead;
            while (bytesLeft > 0)
            {
                bytesLeft -= await stream.ReadAsync(buffer, bytesToRead - bytesLeft, bytesLeft).ConfigureAwait(false);
            }

            this.SetTextureFromBytesArray(buffer);

            shared.Return(buffer);
        }

        private async void SetTextureFromBytesArray(byte[] bytes)
        {
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (var codec = SKCodec.Create(stream))
                {
                    var info = new SKImageInfo
                    {
                        Width = codec.Info.Width,
                        Height = codec.Info.Height,
                        AlphaType = codec.Info.AlphaType,
                        ColorType = SKColorType.Rgba8888,
                    };

                    using (var bitmap = SKBitmap.Decode(codec, info))
                    {
                        var pixelsPtr = bitmap.GetPixels();
                        await EvergineForegroundTask.Run(() =>
                        {
                            this.EnsureVideoTextureIsReady((uint)codec.Info.Width, (uint)codec.Info.Height);
                            this.graphicsContext.UpdateTextureData(this.imageTexture, pixelsPtr, (uint)info.BytesSize, 0);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Streaming texture set error");
            }
        }

        private void EnsureVideoTextureIsReady(uint imageWidth, uint imageHeight)
        {
            if (!this.initializedTexture)
            {
                // Create texture
                var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
                var desc = new TextureDescription()
                {
                    Type = TextureType.Texture2D,
                    Width = imageWidth,
                    Height = imageHeight,
                    Depth = 1,
                    ArraySize = 1,
                    Faces = 1,
                    Usage = ResourceUsage.Default,
                    CpuAccess = ResourceCpuAccess.None,
                    Flags = TextureFlags.ShaderResource | TextureFlags.RenderTarget,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleCount = TextureSampleCount.None,
                };

                this.imageTexture = this.graphicsContext.Factory.CreateTexture(ref desc);
                holographicEffect.Texture = this.imageTexture;
                holographicEffect.Albedo = Color.White;

                // Set Window Size
                this.videoFramePlaneMesh.Width = imageWidth / PixelsInAMeter;
                this.videoFramePlaneMesh.Height = imageHeight / PixelsInAMeter;
                this.windowConfigurator.Size = new Vector2(imageWidth / PixelsInAMeter, (imageHeight / PixelsInAMeter) + BottomMarginForLogo);
                this.windowConfigurator.DisplayFrontPlate = false;

                // Give space to plain concepts logo
                this.transform.LocalPosition = new Vector3(this.transform.LocalPosition.X, this.transform.LocalPosition.Y + (BottomMarginForLogo / 2), this.transform.LocalPosition.Z);

                this.spinnerEntity.IsEnabled = false;
                this.initializedTexture = true;
            }
        }
    }
}
