// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Threading;
using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Threading;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Sliders;
using Evergine.Platform;
using SixLabors.ImageSharp.PixelFormats;
using Xrv.Core.Storage;
using Xrv.Core.UI.Windows;
using Xrv.ImageGallery.Helpers;

namespace Xrv.ImageGallery.Components
{
    /// <summary>
    /// This component shows a gallery of images. Has a slider and a pair of buttons in order to change between an image and another.
    /// </summary>
    public class ImageGallery : Component
    {
        [BindService]
        private readonly GraphicsContext graphicsContext = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_image_gallery_next_pressable_button")]
        private readonly PressableButton nextButton = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_image_gallery_previous_pressable_button")]
        private readonly PressableButton previousButton = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_image_gallery_slider")]
        private readonly PinchSlider slider = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_picture")]
        private readonly MaterialComponent galleryFrameMaterial = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_next", isRecursive: true)]
        private readonly Entity nextButtonEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_previous", isRecursive: true)]
        private readonly Entity previousButtonEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_slider", isRecursive: true)]
        private readonly Entity sliderEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_spinner", isRecursive: true)]
        private readonly Entity spinnerEntity = null;

        private Texture imageTexture = null;
        private int imageIndex = 0;
        private CancellationTokenSource cancellationSource;
        private bool showNavigationButtons = true;
        private bool showNavigationSlider = true;
        private List<FileItem> images = null;
        private WindowConfigurator windowConfigurator = null;

        /// <summary>
        /// Gets or sets the route of the Storage used to get and store the images listed in the gallery.
        /// </summary>
        [IgnoreEvergine]
        public Core.Storage.FileAccess FileAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if the navigation slider is shown below the gallery.
        /// </summary>
        public bool ShowNavigationSlider
        {
            get
            {
                return this.showNavigationSlider;
            }

            set
            {
                if (this.sliderEntity != null)
                {
                    this.sliderEntity.IsEnabled = value;
                    if (value)
                    {
                        this.RecalculateSliderPosition();
                    }
                }

                this.showNavigationSlider = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether if the navigation buttons are shown below the gallery.
        /// </summary>
        public bool ShowNavigationButtons
        {
            get
            {
                return this.showNavigationButtons;
            }

            set
            {
                if (this.nextButtonEntity != null && this.previousButtonEntity != null)
                {
                    this.nextButtonEntity.IsEnabled = value;
                    this.previousButtonEntity.IsEnabled = value;
                }

                this.showNavigationButtons = value;
            }
        }

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

        /// <summary>
        /// Gets or sets the width of the images shown.
        /// </summary>
        public uint ImagePixelsWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the images shown.
        /// </summary>
        public uint ImagePixelsHeight { get; set; }

        /// <summary>
        /// Gets or sets gallery name.
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc/>
        protected async override void OnActivated()
        {
            base.OnActivated();

            var fileList = await this.FileAccess.EnumerateFilesAsync();
            fileList ??= new List<FileItem>();
            this.images = new List<FileItem>(fileList);
            this.ReloadImage();
            this.RecalculateSliderPosition();
            this.windowConfigurator = this.Owner.FindComponentInParents<WindowConfigurator>();
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (base.OnAttached())
            {
                var holographicEffect = new HoloGraphic(this.galleryFrameMaterial.Material);

                TextureDescription desc = new ()
                {
                    Type = TextureType.Texture2D,
                    Width = this.ImagePixelsWidth,
                    Height = this.ImagePixelsHeight,
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

                this.nextButton.ButtonReleased += this.NextButtonReleased;
                this.previousButton.ButtonReleased += this.PreviousButtonReleased;
                this.slider.ValueUpdated += this.SliderValueUpdated;
                this.slider.InteractionEnded += this.SliderInteractionEnded;

                this.nextButtonEntity.IsEnabled = this.ShowNavigationButtons;
                this.previousButtonEntity.IsEnabled = this.ShowNavigationButtons;
                this.sliderEntity.IsEnabled = this.ShowNavigationSlider;
            }

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
            this.RecalculateSliderPosition();
        }

        private void RecalculateSliderPosition()
        {
            if (this.ShowNavigationSlider)
            {
                this.slider.SliderValue = this.ImageIndex / (float)(this.images.Count - 1);
            }
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
            this.RecalculateSliderPosition();
        }

        private void PreviousButtonReleased(object sender, EventArgs e)
        {
            this.ImageIndex--;
            this.RecalculateSliderPosition();
        }

        private void ReloadImage()
        {
            if (this.images.Count == 0)
            {
                return;
            }

            this.LoadRawJPG(this.images[this.ImageIndex].Name);
            if (!Application.Current.IsEditor && this.windowConfigurator != null)
            {
                this.windowConfigurator.Title = $"{this.Name} {this.ImageIndex + 1} of {this.images.Count}";
            }

            if (this.ShowNavigationButtons)
            {
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
        }

        private void LoadRawJPG(string filePath)
        {
            this.cancellationSource?.Cancel();
            this.cancellationSource = new CancellationTokenSource();
            this.spinnerEntity.IsEnabled = true;

            EvergineBackgroundTask.Run(
                async () =>
            {
                AssetsDirectory assetDirectory = Application.Current.Container.Resolve<AssetsDirectory>();
                byte[] data;
                using (var fileStream = await this.FileAccess.GetFileAsync(filePath))
                {
                    using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(fileStream))
                    {
                        RawImageLoader.CopyImageToArrayPool(image, out _, out data);
                    }

                    await EvergineForegroundTask.Run(() => this.graphicsContext.UpdateTextureData(this.imageTexture, data));
                }

                this.spinnerEntity.IsEnabled = false;
            }, this.cancellationSource.Token);
        }
    }
}
