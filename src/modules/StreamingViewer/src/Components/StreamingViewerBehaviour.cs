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

namespace Xrv.StreamingViewer.Components
{
    public class StreamingViewerBehavior : Behavior
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
        private string sourceURL = "http://80.32.125.254:8080/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER";
        private Stream stream;
        public uint VideoPixelsWidth = 1024;
        public uint VideoPixelsHeight = 768;



        protected override void OnActivated()
        {
            base.OnActivated();
            //if (!Application.Current.IsEditor)
            //{
            //    this.GetVideo();
            //}
        }


        protected override bool OnAttached()
        {
            var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
            TextureDescription desc = new TextureDescription()
            {
                Type = TextureType.Texture2D,
                Width = 1024,
                Height = 768,
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
            return base.OnAttached();
        }

        public void GetVideo()
        {
            //texture = new RGBTexture()
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(this.sourceURL);
            WebResponse resp = (WebResponse)req.GetResponse();
            this.stream = resp.GetResponseStream();
            //EvergineBackgroundTask.Run(this.GetFrame);
            //this.GetFrame();
        }

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
                    break;
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
            using (var image2 = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes))
            {
                RawImageLoader.CopyImageToArrayPool(image2, false, out var size, out var newJpgData);
                this.graphicsContext.UpdateTextureData(this.imageTexture, newJpgData);
            }
            // TENEMOS ESTA OPCION TAMBIEN
            ////var texturajpg = this.assetsService.Load<Texture>("a.jpg", ms);
            ////var holographicEffect = new HoloGraphic(this.videoFrameMaterial.Material);
            ////holographicEffect.Texture = texturajpg;
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

        protected override void Update(TimeSpan gameTime)
        {
            if(this.stream == null)
            {
                this.GetVideo();
            }
            this.GetFrame();
        }
    }
}
