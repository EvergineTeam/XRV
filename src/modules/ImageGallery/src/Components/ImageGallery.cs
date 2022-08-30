using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Buffers;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Advanced;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics.Materials;
using Evergine.Components.Graphics3D;
using Evergine.MRTK.Effects;

namespace Xrv.ImageGallery.Components
{
    public class ImageGallery : Component
    {
        [BindService]
        protected GraphicsContext graphicsContext;

        // [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_next")]
        private PressableButton nextButton;
        // [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_previous")]
        private PressableButton previousButton;

        ////[BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_picture")]
        private MaterialComponent galleryFrameMaterial;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_picture")]
        private Entity galleryFrameEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_next")]
        private Entity nextButtonEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_previous")]
        private Entity previousButtonEntity;

        private int imageIndex = 0;

        private Texture imageTexture = null;

        public uint imagePixelsWidth = 100;
        public uint imagePixelsHeight = 100;

        protected override bool OnAttached()
        {
            this.nextButton = this.nextButtonEntity.FindComponentInChildren<PressableButton>();
            this.previousButton = this.previousButtonEntity.FindComponentInChildren<PressableButton>();
            if (this.galleryFrameMaterial == null)
            {
                this.galleryFrameMaterial = this.galleryFrameEntity.FindComponent<MaterialComponent>();

                var holographicEffect = new HoloGraphic(this.galleryFrameMaterial.Material);
                // this.imageTexture = holographicEffect.Texture;

                TextureDescription desc = new TextureDescription()
                {
                    Type = TextureType.Texture2D,
                    Width = this.imagePixelsWidth,
                    Height = this.imagePixelsHeight,
                    Depth = 1,
                    ArraySize = 1,
                    Faces = 1,
                    Usage = ResourceUsage.Default,
                    CpuAccess = ResourceCpuAccess.None,
                    Flags = TextureFlags.ShaderResource,
                    Format = PixelFormat.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    SampleCount = TextureSampleCount.None,
                };

                this.imageTexture = this.graphicsContext.Factory.CreateTexture(ref desc);
                holographicEffect.Texture = this.imageTexture;


            }

            this.nextButton.ButtonPressed += this.onNextButtonPressed;
            this.previousButton.ButtonPressed += this.onPreviousButtonPressed;
            return base.OnAttached();
        }

        protected override void OnDetach()
        {
            this.nextButton.ButtonPressed -= this.onNextButtonPressed;
            this.previousButton.ButtonPressed -= this.onPreviousButtonPressed;
            base.OnDetach();
        }

        public void onNextButtonPressed(object sender, EventArgs e)
        {
            Debug.WriteLine("NEXT");
            this.LoadRawJPG("XRV/Textures/TestImages/test1.jpg");
        }

        public void onPreviousButtonPressed(object sender, EventArgs e)
        {
            Debug.WriteLine("PREVIOUS");
            this.LoadRawJPG("XRV/Textures/TestImages/test2.jpg");
        }

        private void LoadRawJPG(string filePath)
        {
            AssetsDirectory assetDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
            byte[] data;
            using (var fileStream = assetDirectory.Open(filePath))
            {
                using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                {
                    this.CopyImageToArrayPool(image, false, out _, out data);
                }

                this.graphicsContext.UpdateTextureData(this.imageTexture, data);
            }
        }

        private void CopyImageToArrayPool(Image<Rgba32> image, bool premultiplyAlpha, out int dataLength, out byte[] data)
        {
            var bytesPerPixel = image.PixelType.BitsPerPixel / 8;
            dataLength = image.Width * image.Height * bytesPerPixel;
            data = ArrayPool<byte>.Shared.Rent(dataLength);
            var dataPixels = MemoryMarshal.Cast<byte, Rgba32>(data);
            if (image.DangerousTryGetSinglePixelMemory(out var pixels))
            {
                if (premultiplyAlpha)
                {
                    this.CopyToPremultiplied(pixels.Span, dataPixels);
                }
                else
                {
                    pixels.Span.CopyTo(dataPixels);
                }
            }
            else
            {
                for (int i = 0; i < image.Height; i++)
                {
                    var row = image.DangerousGetPixelRowMemory(i);
                    if (premultiplyAlpha)
                    {
                        this.CopyToPremultiplied(row.Span, dataPixels.Slice(i * image.Width, image.Width));
                    }
                    else
                    {
                        row.Span.CopyTo(dataPixels.Slice(i * image.Width, image.Width));
                    }
                }
            }
        }

        private void CopyToPremultiplied(Span<Rgba32> pixels, Span<Rgba32> destination)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                ref Rgba32 pixel = ref pixels[i];
                ref Rgba32 destinationPixel = ref destination[i];
                ref var a = ref pixel.A;
                if (a == 0)
                {
                    destinationPixel.PackedValue = 0;
                }
                else
                {
                    destinationPixel.R = (byte)((pixel.R * a) >> 8);
                    destinationPixel.G = (byte)((pixel.G * a) >> 8);
                    destinationPixel.B = (byte)((pixel.B * a) >> 8);
                    destinationPixel.A = pixel.A;
                }
            }
        }
    }
}
