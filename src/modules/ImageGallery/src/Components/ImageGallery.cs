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
using Evergine.MRTK.SDK.Features.UX.Components.Sliders;

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

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_image_gallery_slider")]
        private PinchSlider slider;

        ////[BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_picture")]
        private MaterialComponent galleryFrameMaterial;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_picture")]
        private Entity galleryFrameEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_next")]
        private Entity nextButtonEntity;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_previous")]
        private Entity previousButtonEntity;

        private int _imageIndex = 0;

        public int ImageIndex
        {
            get
            {
                return this._imageIndex;
            }

            set
            {
                if (value >= 0)
                {
                    if (this.images != null && value < this.images.Count)
                    {
                        this._imageIndex = value;
                    }
                }
            }
        }

        public uint imagePixelsWidth = 640;
        public uint imagePixelsHeight = 640;

        private Texture imageTexture = null;

        public List<string> images = null;

        protected override bool OnAttached()
        {
            if (this.images == null)
            {
                string[] array = { "XRV/Textures/TestImages/test1.jpg", "XRV/Textures/TestImages/test2.jpg", "XRV/Textures/TestImages/test3.jpg" };
                this.images = new List<string>(array);
            }

            this.nextButton = this.nextButtonEntity.FindComponentInChildren<PressableButton>();
            this.previousButton = this.previousButtonEntity.FindComponentInChildren<PressableButton>();

            if (this.galleryFrameMaterial == null)
            {
                this.galleryFrameMaterial = this.galleryFrameEntity.FindComponent<MaterialComponent>();

                var holographicEffect = new HoloGraphic(this.galleryFrameMaterial.Material);

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
                this.ReloadImage();
            }

            this.nextButton.ButtonPressed += this.NextButtonPressed;
            this.previousButton.ButtonPressed += this.PreviousButtonPressed;
            this.slider.ValueUpdated += this.SliderValueUpdated;
            return base.OnAttached();
        }

        protected override void OnDetach()
        {
            this.nextButton.ButtonPressed -= this.NextButtonPressed;
            this.previousButton.ButtonPressed -= this.PreviousButtonPressed;
            base.OnDetach();
        }

        private void SliderValueUpdated(object sender, SliderEventData e)
        {
            Debug.WriteLine(e.NewValue);
            // throw new NotImplementedException();
        }

        private void NextButtonPressed(object sender, EventArgs e)
        {
            this.ImageIndex++;
            this.ReloadImage();
        }

        private void PreviousButtonPressed(object sender, EventArgs e)
        {
            this.ImageIndex--;
            this.ReloadImage();
        }

        private void ReloadImage()
        {
            this.LoadRawJPG(this.images[this.ImageIndex]);

            if (this.ImageIndex == 0)
            {
                this.previousButtonEntity.IsEnabled = false;
            }
            else
            {
                this.previousButtonEntity.IsEnabled = true;
            }

            if (this.ImageIndex >= this.images.Count - 1)
            {
                this.nextButtonEntity.IsEnabled = false;
            }
            else
            {
                this.nextButtonEntity.IsEnabled = true;
            }
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
