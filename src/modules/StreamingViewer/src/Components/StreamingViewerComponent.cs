// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using System.Diagnostics;
using System.Collections;
using Evergine.MRTK.Effects;
using Evergine.Framework.Graphics;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Imaging;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using PixelFormat = Evergine.Common.Graphics.PixelFormat;
using Application = Evergine.Framework.Application;
using System.Net;
using Xrv.ImageGallery.Helpers;
using SixLabors.ImageSharp.ColorSpaces;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Azure.Core;
using static BulletSharp.DiscreteCollisionDetectorInterface;
//using AForge.Video;

namespace Xrv.StreamingViewer.Components
{
    public class StreamingViewerComponent : Component
    {
        [BindService]
        private readonly GraphicsContext graphicsContext = null;

        [BindService]
        private readonly AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_video_frame")]
        private readonly MaterialComponent videoFrameMaterial = null;

        private Texture imageTexture = null;
        public MeshRenderer frame;
        //private string sourceURL = "http://80.32.125.254:8080/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER";
        //private string sourceURL = "http://213.193.89.202/mjpg/video.mjpg"; THIS ONE SHOULD FAIL BY AUTH
        //private string sourceURL = "http://161.72.22.244/mjpg/video.mjpg?timestamp=1668154449782";
        //private string sourceURL = "http://153.142.212.238:8081/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER";
        //private string sourceURL = "http://85.93.226.157:8082/mjpg/video.mjpg";

        public string SourceURL { get; set; }

        private Stream stream;
        public uint VideoPixelsWidth = 1280;
        public uint VideoPixelsHeight = 720;

        private bool initializedTexture = false;

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
            ////Debug.WriteLine("NEW FRAME");
            ////Debug.WriteLine(bytesToRead);
            int bytesLeft = bytesToRead;
            MemoryStream memoryStream = new MemoryStream();
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
                    var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
                    TextureDescription desc = new TextureDescription()
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
        private void ReadStream(Stream stream)
        {
            int streamByte;
            var streamText = string.Empty;
            int i = 0;
            while (i < 1000)
            {
                streamByte = stream.ReadByte();
                streamText += (char)streamByte;
                i++;
            }

            Debug.WriteLine(streamText);
        }
    }
}
