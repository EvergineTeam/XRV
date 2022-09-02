// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Threading;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Sliders;
using Evergine.Platform;
using SixLabors.ImageSharp.PixelFormats;
using Xrv.ImageGallery.Helpers;

namespace Xrv.ImageGallery.Components
{
    /// <summary>
    /// This component shows a gallery of images. Has a slider and a pair of buttons in order to change between an image and another.
    /// </summary>
    public class ImageGallery : Component
    {
        /// <summary>
        /// Width of the images shown.
        /// </summary>
        public uint imagePixelsWidth = 640;

        /// <summary>
        /// Height of the images shown.
        /// </summary>
        public uint imagePixelsHeight = 640;

        /// <summary>
        /// List of paths to the images to show in the Gallery.
        /// </summary>
        public List<string> images = null;

        private Texture imageTexture = null;

        [BindService]
        private GraphicsContext graphicsContext = null;
        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_next_pressable_button")]
        private PressableButton nextButton = null;
        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_previous_pressable_button")]
        private PressableButton previousButton = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_image_gallery_slider")]
        private PinchSlider slider = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_picture")]
        private MaterialComponent galleryFrameMaterial = null;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_next")]
        private Entity nextButtonEntity = null;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_image_gallery_previous")]
        private Entity previousButtonEntity = null;

        private int imageIndex = 0;
        private CancellationTokenSource cancellationSource;

        /// <summary>
        /// Gets or sets the index of the image that is showing the gallery at the moment.
        /// </summary>
        public int ImageIndex
        {
            get
            {
                return this.imageIndex;
            }

            set
            {
                if (value >= 0)
                {
                    if (this.images != null && value < this.images.Count)
                    {
                        this.imageIndex = value;
                        this.ReloadImage();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (this.images == null)
            {
                string[] array = { "XRV/Textures/TestImages/test1.jpg", "XRV/Textures/TestImages/test2.jpg", "XRV/Textures/TestImages/test3.jpg" };
                this.images = new List<string>(array);
            }

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
            this.recalculateSliderPosition();

            this.nextButton.ButtonReleased += this.NextButtonReleased;
            this.previousButton.ButtonReleased += this.PreviousButtonReleased;
            this.slider.ValueUpdated += this.SliderValueUpdated;
            this.slider.InteractionEnded += this.SliderInteractionEnded;
            return base.OnAttached();
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            this.nextButton.ButtonReleased -= this.NextButtonReleased;
            this.previousButton.ButtonReleased -= this.PreviousButtonReleased;
            base.OnDetach();
        }

        private void SliderInteractionEnded(object sender, EventArgs e)
        {
            this.recalculateSliderPosition();
        }

        private void recalculateSliderPosition()
        {
            this.slider.SliderValue = this.ImageIndex / (float)(this.images.Count - 1);
        }

        private void SliderValueUpdated(object sender, SliderEventData e)
        {
            var newImageIndex = (int)Math.Round(e.NewValue * (this.images.Count - 1));
            if (newImageIndex != this.ImageIndex)
            {
                this.ImageIndex = newImageIndex;
            }
        }

        private void NextButtonReleased(object sender, EventArgs e)
        {
            this.ImageIndex++;
            this.recalculateSliderPosition();
        }

        private void PreviousButtonReleased(object sender, EventArgs e)
        {
            this.ImageIndex--;
            this.recalculateSliderPosition();
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
            this.cancellationSource?.Cancel();
            this.cancellationSource = new CancellationTokenSource();

            EvergineBackgroundTask.Run(
            () =>
            {
                AssetsDirectory assetDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
                byte[] data;
                using (var fileStream = assetDirectory.Open(filePath))
                {
                    using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                    {
                        RawImageLoader.CopyImageToArrayPool(image, false, out _, out data);
                    }

                    this.graphicsContext.UpdateTextureData(this.imageTexture, data);
                }
            }, this.cancellationSource.Token);
        }
    }
}
