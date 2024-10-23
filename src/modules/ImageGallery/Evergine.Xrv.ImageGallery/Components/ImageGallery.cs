// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Common.IO;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Threading;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.Sliders;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Xrv.Core.UI.Windows;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Evergine.Xrv.ImageGallery.Components
{
    /// <summary>
    /// This component shows a gallery of images. Has a slider and a pair of buttons in order to change between an image and another.
    /// </summary>
    public class ImageGallery : Component
    {
        [BindService]
        private GraphicsContext graphicsContext = null;

        [BindService]
        private LocalizationService localization = null;

        [BindService]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_picture")]
        private MaterialComponent galleryFrameMaterial = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_next", isRecursive: true)]
        private Entity nextButtonEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_previous", isRecursive: true)]
        private Entity previousButtonEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_slider", isRecursive: true)]
        private Entity sliderEntity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_image_gallery_spinner", isRecursive: true)]
        private Entity spinnerEntity = null;

        private PinchSlider slider = null;
        private PressableButton nextButton = null;
        private PressableButton previousButton = null;

        private Texture imageTexture = null;
        private int imageIndex = 0;
        private CancellationTokenSource cancellationSource = null;
        private bool showNavigationButtons = true;
        private bool showNavigationSlider = true;
        private bool isVisuallyEnabled = true;
        private List<FileItem> images = null;
        private WindowConfigurator windowConfigurator = null;
        private ILogger logger = null;

        /// <summary>
        /// Raised when presented image has changed.
        /// </summary>
        public event EventHandler CurrentImageChanged = null;

        /// <summary>
        /// Gets or sets the route of the Storage used to get and store the images listed in the gallery.
        /// </summary>
        [IgnoreEvergine]
        public FileAccess FileAccess { get; set; }

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
                    if (this.HasImages && value < this.images.Count)
                    {
                        this.imageIndex = value;
                        this.ReloadImage();
                        this.UpdateUIElements();
                        this.CurrentImageChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets gallery number of images.
        /// </summary>
        public int NumberOfImages { get => this.images?.Count ?? 0; }

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

        /// <summary>
        /// Gets a value indicating whether gallery contains any image.
        /// </summary>
        public bool HasImages { get => this.images?.Any() == true; }

        /// <summary>
        /// Gets or sets a value indicating whether gallery UI elements should
        /// be enabled or disabled.
        /// </summary>
        public bool IsVisuallyEnabled
        {
            get => this.isVisuallyEnabled;
            set
            {
                if (this.isVisuallyEnabled != value)
                {
                    this.isVisuallyEnabled = value;
                    this.UpdateUIElements();
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;

                this.nextButton = this.nextButtonEntity.FindComponentInChildren<PressableButton>(isRecursive: true);
                this.previousButton = this.previousButtonEntity.FindComponentInChildren<PressableButton>(isRecursive: true);
                this.slider = this.sliderEntity.FindComponentInChildren<PinchSlider>(isRecursive: true);

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

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.windowConfigurator = this.Owner.FindComponentInParents<WindowConfigurator>();

            _ = this.LoadFilesOnInitAsync();
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            this.nextButton.ButtonReleased -= this.NextButtonReleased;
            this.previousButton.ButtonReleased -= this.PreviousButtonReleased;
            base.OnDetach();
        }

        private async Task LoadFilesOnInitAsync()
        {
            try
            {
                if (this.FileAccess != null)
                {
                    var fileList = await this.FileAccess.EnumerateFilesAsync();
                    fileList ??= new List<FileItem>();
                    this.images = new List<FileItem>(fileList);
                }
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error requesting list of images for gallery");
            }

            this.ReloadImage();
            this.RecalculateSliderPosition();
            this.UpdateUIElements();
        }

        private void SliderInteractionEnded(object sender, EventArgs e)
        {
            if (!Application.Current.IsEditor)
            {
                this.RecalculateSliderPosition();
            }
        }

        private void RecalculateSliderPosition()
        {
            bool shouldShowSlider = this.HasImages && this.images.Count > 1;
            this.slider.IsEnabled = shouldShowSlider;

            if (this.ShowNavigationSlider && shouldShowSlider)
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
            if (!this.HasImages)
            {
                return;
            }

            this.LoadRawJPG(this.images[this.ImageIndex].Name);
            if (!Application.Current.IsEditor && this.windowConfigurator != null)
            {
                this.windowConfigurator.LocalizedTitle = () =>
                {
                    var title = string.Format(
                        this.localization.GetString(() => Resources.Strings.Window_Title_WithCount),
                        this.ImageIndex + 1,
                        this.images.Count);
                    return title;
                };
            }
        }

        private void UpdateUIElements()
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            if (!this.HasImages)
            {
                this.previousButtonEntity.IsEnabled = this.nextButtonEntity.IsEnabled = false;
                this.spinnerEntity.IsEnabled = false;
                return;
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

                this.nextButtonEntity.FindComponentInChildren<VisuallyEnabledController>().IsVisuallyEnabled = this.isVisuallyEnabled;
                this.previousButtonEntity.FindComponentInChildren<VisuallyEnabledController>().IsVisuallyEnabled = this.isVisuallyEnabled;
            }

            var sliderCollider = this.slider.Owner.FindComponentInChildren<Collider3D>(isExactType: false);
            var sliderBody = this.slider.Owner.FindComponentInChildren<StaticBody3D>();
            sliderCollider.IsEnabled = sliderBody.IsEnabled = this.isVisuallyEnabled;
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
                using (var fileStream = await this.FileAccess.GetFileAsync(filePath))
                using (var codec = SKCodec.Create(fileStream))
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
                        await EvergineForegroundTask.Run(() => this.graphicsContext.UpdateTextureData(this.imageTexture, pixelsPtr, (uint)info.BytesSize, 0));
                    }
                }

                this.spinnerEntity.IsEnabled = false;
            }, this.cancellationSource.Token);
        }
    }
}
