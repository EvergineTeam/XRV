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
        private Texture texture;
        //private string sourceURL = "http://80.32.125.254:8080/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER";
        //private string sourceURL = "http://213.193.89.202/mjpg/video.mjpg";
        //private string sourceURL = "http://161.72.22.244/mjpg/video.mjpg?timestamp=1668154449782";
        private string sourceURL = "http://153.142.212.238:8081/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER";
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

        private WebResponse tryConnection(IAsyncResult ar, int tryNum = 0)
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
                    return this.tryConnection(ar, tryNum + 1);
                }
            }
            else
            {
                Debug.WriteLine("Connection error");
                return null;
            }
        }

        public void GetVideo()
        {
            ////texture = new RGBTexture()
            //HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.sourceURL);
            //WebResponse resp = (WebResponse)req.GetResponse();
            //this.stream = resp.GetResponseStream();
            //EvergineBackgroundTask.Run(this.GetFrame);

            WebRequest req = WebRequest.Create(this.sourceURL);
            req.BeginGetResponse(
                ar =>
                    {
                // TODO: Add exception handling: EndGetResponse could throw

                using (var response = req.EndGetResponse(ar))
                {
                    // using (var reader = new StreamReader(response.GetResponseStream()))
                    using (var responseStream = response.GetResponseStream())
                    {
                        int responseByte;
                        bool atEndOfLine = false;
                        string line = "";
                        int size = 0;
                        // This loop goes as long as twitter is streaming
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
                                    //return result;
                                    //Read all
                                    this.readStreaming(responseStream, size);
                                    atEndOfLine = false;
                                    line = "";
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
                                    line = "";
                                }


                            }
                            else
                            {
                                atEndOfLine = false;
                                line += (char)responseByte;
                            }


                            //var in = reader.Read()
                            ////MemoryStream memoryStream = new MemoryStream();


                            ////reader.Read(memoryStream.GetBuffer, 0, )
                            ////if (FindLength(stream))
                            ////    //memoryStream.Write()
                            ////    var line = reader.ReadLine();
                            ////if (line.ToLower().Contains("image/jpeg".ToLower()))
                            ////{
                            ////    Debug.WriteLine("new frame");
                            ////}
                            //// Debug.WriteLine(line);
                        }
                    }
                }
            }, req);
            //Console.ReadLine();


            //this.GetFrame();
            Debug.WriteLine("Starting");
            ////MJPEGStream stream = new MJPEGStream(this.sourceURL);
            ////stream.NewFrame += new NewFrameEventHandler(StreamNewFrame);
            ////stream.Start();

        }

        private void readStreaming(Stream responseStream, int bytesToRead)
        {
            int bytesLeft = bytesToRead;
            MemoryStream memoryStream = new MemoryStream();
            byte[] buffer = new byte[bytesToRead];
            while (bytesLeft > 0)
            {
                bytesLeft -= responseStream.Read(buffer, bytesToRead - bytesLeft, bytesLeft);
                //yield return null;
            }
            //responseStream.Position = bytesToRead;
            Debug.WriteLine("NEW FRAME");
            this.SetTextureFromBytesArray(buffer);
        }


        ////private void StreamNewFrame(object sender, NewFrameEventArgs eventArgs)
        ////{
        ////    Debug.WriteLine("New Frame");
        ////}

        private void GetFrame()
        {
            ////while ((this.stream = resp.GetResponseStream()) != null)
            ////{
            bool streamFinished = false;


            ////var fileStream = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "testimagestream.txt");
            //////this.stream.Seek(0, SeekOrigin.Begin);
            ////this.stream.CopyTo(fileStream);
            ////fileStream.Close();

            byte[] jpegData = new byte[5000000];

            //// this.ReadStream(this.stream);
            while (!streamFinished)
            {
                int bytesToRead = this.FindLength(this.stream);
                Debug.WriteLine(bytesToRead);
                if (bytesToRead == -1)
                {
                    Debug.WriteLine("End of Stream");
                    streamFinished = true;
                    //yield break;
                }

                int bytesLeft = bytesToRead;

                while (bytesLeft > 0)
                {
                    bytesLeft -= stream.Read(jpegData, bytesToRead - bytesLeft, bytesLeft);
                    //yield return null;
                }

                //this.stream.Read(jpegData, 0, bytesLeft);
                this.SetTextureFromBytesArray(jpegData);




                //var fileStream = File.Create(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "testimagestream.txt");
                ////var fileStream = File.Create("C:\\Users\\rafael.caro\\testimagestream.txt");
                ////fileStream.Write(ms.GetBuffer(), 0, bytesToRead);
                ////fileStream.Close();
                ////ms.Position = 0;

                //////this.graphicsContext.UpdateTextureData(this.imageTexture, jpegData);


                //////this.graphicsContext.UpdateTextureData(this.imageTexture, ms.GetBuffer());

                //////var image = System.Drawing.Image.FromStream(ms);

                //////image.Save("output.jpg", ImageFormat.Jpeg);

                //////MemoryStream ms2 = new MemoryStream();
                ////int size = 0;
                ////byte[] newJpgData = null;

                ////// DE AQUI PALANTE FUNCIONA
                ////using (var image2 = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(ms))
                ////{
                ////    RawImageLoader.CopyImageToArrayPool(image2, false, out size, out newJpgData);
                ////}


                ////var newArray = new byte[size];
                ////Array.Copy(newJpgData, newArray, size);

                ////////for (int i = 0; i<newArray.Length; i += 4)
                ////////{
                ////////    newArray[0 + i] = 0;
                ////////    newArray[1 + i] = 0;
                ////////    newArray[2 + i] = 255;
                ////////    newArray[3 + i] = 255;
                ////////}

                ////this.graphicsContext.UpdateTextureData(this.imageTexture, newArray);

                //////image.Save(ms2, ImageFormat.ra);

                ////////texture.LoadImage(ms.GetBuffer());
                ////////frame.material.mainTexture = texture;
                ////stream.ReadByte(); // CR after bytes
                ////stream.ReadByte(); // LF after bytes
            }
            //}
        }

        private void SetTextureFromBytesArray(byte[] bytes)
        {
            try
            {
                using (var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes))
                {
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
            } catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private void ReadStream(Stream stream)
        {
            int streamByte;
            var streamText = "";
            int i = 0;
            while (i < 1000)
            {
                streamByte = stream.ReadByte();
                streamText += (char)streamByte;
                i++;
            }

            Debug.WriteLine(streamText);
        }

        private int FindLength(Stream stream)
        {
            int streamByte;
            string line = "";
            int result = -1;
            bool atEndOfLine = false;

            while ((streamByte = stream.ReadByte()) != -1)
            {
                // Ignore if Line Feed
                if (streamByte == 10)
                {
                    continue;
                }

                // Check if Carriage Return (We will start a new line)
                if (streamByte == 13)
                {
                    // Check if two blank lines (end of header)
                    if (atEndOfLine)
                    {
                        stream.ReadByte();
                        return result;
                    }

                    if (line.ToLower().StartsWith("content-length:"))
                    {
                        result = Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                    }
                    else
                    {
                        line = "";
                    }

                    atEndOfLine = true;
                }
                else
                {
                    atEndOfLine = false;
                    line += (char)streamByte;
                }
            }

            return -1;
        }
    }
}
