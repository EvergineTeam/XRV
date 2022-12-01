﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xrv.Core.UI.Windows;
using Xrv.StreamingViewer.Helpers;
using Application = Evergine.Framework.Application;
using PixelFormat = Evergine.Common.Graphics.PixelFormat;

namespace Xrv.StreamingViewer.Components
{
    /// <summary>
    /// Module that shows a live video streaming.
    /// </summary>
    public class StreamingViewerComponent : Component
    {
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
        private byte[] textureBuffer;
        private bool stop;

        private const float PixelsInAMeter = 2000f;
        private const float BottomMarginForLogo = 0.05f;

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public string SourceURL { get; set; }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.windowConfigurator = this.Owner.FindComponentInParents<WindowConfigurator>();
            if (!Application.Current.IsEditor)
            {
                this.connectionErrorTextEntity.IsEnabled = false;
                this.stop = false;
                this.GetVideo();
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.stop = true;
        }

        private void GetVideo()
        {
            WebRequest req = WebRequest.Create(this.SourceURL);
            req.BeginGetResponse(
                async
                ar =>
                {
                    try
                    {
                        using var response = req.EndGetResponse(ar);
                        using var responseStream = response.GetResponseStream();
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
                                    await this.ReadStreaming(responseStream, size);
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
                        Debug.WriteLine("Connection error");
                        Debug.WriteLine(ex);
                        this.connectionErrorTextEntity.IsEnabled = true;
                        this.spinnerEntity.IsEnabled = false;
                    }
                }, req);
        }

        private async Task ReadStreaming(Stream responseStream, int bytesToRead)
        {
            var shared = ArrayPool<byte>.Shared;
            var buffer = shared.Rent(bytesToRead);

            int bytesLeft = bytesToRead;
            while (bytesLeft > 0)
            {
                bytesLeft -= responseStream.Read(buffer, bytesToRead - bytesLeft, bytesLeft);
                await Task.Delay(100);
            }

            this.SetTextureFromBytesArray(buffer);

            shared.Return(buffer);
        }

        private async void SetTextureFromBytesArray(byte[] bytes)
        {
            try
            {
                using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes))
                {
                    // Option 1
                    RawImageLoader.CopyImageToArrayPool(image, out var size, out this.textureBuffer);

                    // Option 2
                    ////var iMemoryGroup = image.GetPixelMemoryGroup();
                    ////var memoryGroup = iMemoryGroup.ToArray()[0];
                    ////var data = MemoryMarshal.AsBytes(memoryGroup.Span).ToArray();

                    await EvergineForegroundTask.Run(() =>
                    {
                        if (!this.initializedTexture)
                        {
                            // Create texture
                            var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
                            var desc = new TextureDescription()
                            {
                                Type = TextureType.Texture2D,
                                Width = (uint)image.Width,
                                Height = (uint)image.Height,
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
                            this.videoFramePlaneMesh.Width = image.Width / PixelsInAMeter;
                            this.videoFramePlaneMesh.Height = image.Height / PixelsInAMeter;
                            this.windowConfigurator.Size = new Vector2(image.Width / PixelsInAMeter, (image.Height / PixelsInAMeter) + BottomMarginForLogo);
                            this.windowConfigurator.DisplayFrontPlate = false;

                            // Give space to plain concepts logo
                            this.transform.LocalPosition = new Vector3(this.transform.LocalPosition.X, this.transform.LocalPosition.Y + (BottomMarginForLogo / 2), this.transform.LocalPosition.Z);

                            this.spinnerEntity.IsEnabled = false;
                            this.initializedTexture = true;
                        }

                        this.graphicsContext.UpdateTextureData(this.imageTexture, this.textureBuffer);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        // For Debug only. Transform stream to plain text
        ////private void ReadStream(Stream stream)
        ////{
        ////    int streamByte;
        ////    var streamText = string.Empty;
        ////    int i = 0;
        ////    while (i < 1000)
        ////    {
        ////        streamByte = stream.ReadByte();
        ////        streamText += (char)streamByte;
        ////        i++;
        ////    }

        ////    Debug.WriteLine(streamText);
        ////}
    }
}