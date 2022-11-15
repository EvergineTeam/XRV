// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Xrv.ImageGallery.Helpers;
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

        private Texture imageTexture = null;
        private bool initializedTexture = false;

        /// <summary>
        /// Event fired when the size of the streaming has changed.
        /// </summary>
        public event EventHandler<Vector2> StreamingImageSizeUpdated;

        /// <summary>
        /// Gets or sets the URL of the source of the streaming.
        /// </summary>
        public string SourceURL { get; set; }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            if (!Application.Current.IsEditor)
            {
                this.GetVideo();
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            return base.OnAttached();
        }

        // This will try to establish connections five times before give an error
        private WebResponse TryConnection(IAsyncResult ar, int tryNum = 0)
        {
            if (tryNum < 5)
            {
                WebResponse response;
                try
                {
                    var req = (WebRequest)ar.AsyncState;
                    response = req.EndGetResponse(ar);
                    return response;
                }
                catch (Exception)
                {
                    return this.TryConnection(ar, tryNum + 1);
                }
            }
            else
            {
                Debug.WriteLine("Connection error");
                return null;
            }
        }

        private void GetVideo()
        {
            WebRequest req = WebRequest.Create(this.SourceURL);
            req.BeginGetResponse(
                ar =>
                {
                    // TODO: Add exception handling: EndGetResponse could throw
                    using var response = req.EndGetResponse(ar);

                    // using (var reader = new StreamReader(response.GetResponseStream()))
                    using var responseStream = response.GetResponseStream();
                    int responseByte;
                    bool atEndOfLine = false;
                    string line = string.Empty;
                    int size = 0;

                    // This loop goes as long as streaming is on
                    while ((responseByte = responseStream.ReadByte()) != -1)
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
                                this.ReadStreaming(responseStream, size);
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
                }, req);
            Debug.WriteLine("Starting");
        }

        private void ReadStreaming(Stream responseStream, int bytesToRead)
        {
            int bytesLeft = bytesToRead;
            byte[] buffer = new byte[bytesToRead];
            while (bytesLeft > 0)
            {
                bytesLeft -= responseStream.Read(buffer, bytesToRead - bytesLeft, bytesLeft);
            }

            this.SetTextureFromBytesArray(buffer);
        }

        private void SetTextureFromBytesArray(byte[] bytes)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);
                if (!this.initializedTexture)
                {
                    // Create texture
                    var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
                    TextureDescription desc = new ()
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

                    // Set Window Size
                    var ownerTransform = this.Owner.FindComponent<Transform3D>();
                    this.videoFramePlaneMesh.Width = image.Width / 2000f;
                    this.videoFramePlaneMesh.Height = image.Height / 2000f;
                    this.StreamingImageSizeUpdated.Invoke(this, new Vector2(image.Width, image.Height));

                    this.initializedTexture = true;
                }

                RawImageLoader.CopyImageToArrayPool(image, false, out var size, out var newJpgData);
                this.graphicsContext.UpdateTextureData(this.imageTexture, newJpgData);
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
