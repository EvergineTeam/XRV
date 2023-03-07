// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK;
using Evergine.MRTK.Effects;
using Evergine.Xrv.Core.Extensions;
using System;
using System.Linq;

namespace Evergine.Xrv.Core.Themes
{
    /// <summary>
    /// Themes support system.
    /// </summary>
    public class ThemesSystem
    {
        private readonly AssetsService assetsService;
        private readonly GraphicsContext graphicsContext;
        private readonly Guid[] frontPlateMaterialIds;
        private readonly ProximityLightDiff frontPlateProximityLight;
        private readonly ProximityLightDiff buttonContentCageProximityLight;

        private Theme currentTheme;
        private bool notificationsEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemesSystem"/> class.
        /// </summary>
        /// <param name="assetsService">Assets service instance.</param>
        /// <param name="graphicsContext">Graphics context instance.</param>
        public ThemesSystem(AssetsService assetsService, GraphicsContext graphicsContext)
        {
            this.assetsService = assetsService;
            this.graphicsContext = graphicsContext;
            this.frontPlateMaterialIds = new[]
            {
                CoreResourcesIDs.Materials.FrontPlateMaterial,
                CoreResourcesIDs.Materials.BorderlessFrontPlate,
            };
            this.frontPlateProximityLight = new ProximityLightDiff
            {
                CenterDiff = new Vector3(7, 46, -12),
                MiddleDiff = new Vector3(9, 56, -23),
                OuterDiff = new Vector3(-6, 112, 101),
            };
            this.buttonContentCageProximityLight = new ProximityLightDiff
            {
                CenterDiff = new Vector3(-39, -26, -18),
                MiddleDiff = new Vector3(171, -16, -23),
                OuterDiff = new Vector3(-67, 83, 53),
            };
        }

        /// <summary>
        /// Raised when a color from current theme is updated; or theme instance has been changed.
        /// </summary>
        public event EventHandler<ThemeUpdatedEventArgs> ThemeUpdated;

        /// <summary>
        /// Gets or sets current theme.
        /// </summary>
        public Theme CurrentTheme
        {
            get => this.currentTheme;
            set
            {
                if (this.CurrentTheme != value)
                {
                    this.UnsubscribePaletteChanges();
                    this.currentTheme = value ?? Theme.Default.Value;
                    this.Refresh();
                    this.SubscribePaletteChanges();
                    this.NotifyGlobalThemeUpdate();
                }
            }
        }

        /// <summary>
        /// Forces a theme refresh.
        /// </summary>
        public void Refresh()
        {
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor1, ThemeColor.PrimaryColor1);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor2, ThemeColor.PrimaryColor2);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor3, ThemeColor.PrimaryColor3);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor1, ThemeColor.SecondaryColor1);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor2, ThemeColor.SecondaryColor2);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor3, ThemeColor.SecondaryColor3);
            this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor4, ThemeColor.SecondaryColor4);
            this.UpdateFrontPlaneGradientTexture();
            this.UpdateAllMrtkMaterials();
        }

        internal void Load()
        {
            if (this.currentTheme == null)
            {
                this.CurrentTheme = Theme.Default.Value;
            }
        }

        private void SubscribePaletteChanges()
        {
            if (this.currentTheme != null)
            {
                this.notificationsEnabled = true;
                this.currentTheme.ColorChanged += this.CurrentTheme_ColorChanged;
            }
        }

        private void UnsubscribePaletteChanges()
        {
            if (this.currentTheme != null)
            {
                this.notificationsEnabled = false;
                this.currentTheme.ColorChanged -= this.CurrentTheme_ColorChanged;
            }
        }

        private void CurrentTheme_ColorChanged(object sender, ThemeColor color)
        {
            this.UpdateMrtkMaterialsByThemeColor(color);

            switch (color)
            {
                case ThemeColor.PrimaryColor1:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor1, ThemeColor.PrimaryColor1);
                    break;
                case ThemeColor.PrimaryColor2:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor2, ThemeColor.PrimaryColor2);
                    break;
                case ThemeColor.PrimaryColor3:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.PrimaryColor3, ThemeColor.PrimaryColor3);
                    break;
                case ThemeColor.SecondaryColor1:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor1, ThemeColor.SecondaryColor1);
                    break;
                case ThemeColor.SecondaryColor2:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor2, ThemeColor.SecondaryColor2);
                    break;
                case ThemeColor.SecondaryColor3:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor3, ThemeColor.SecondaryColor3);
                    break;
                case ThemeColor.SecondaryColor4:
                    this.ChangeColorAndNotify(CoreResourcesIDs.Materials.SecondaryColor4, ThemeColor.SecondaryColor4);
                    this.UpdateAllProximityLights();
                    this.UpdateFrontPlaneGradientTexture();
                    break;
                case ThemeColor.SecondaryColor5:
                    this.UpdateFrontPlaneGradientTexture();
                    this.NotifyColorUpdate(ThemeColor.SecondaryColor5);
                    break;
            }
        }

        private void UpdateHoloGraphicAlbedo(Guid materialId, Color color) =>
            this.assetsService.UpdateHoloGraphicAlbedo(materialId, color);

        private void ChangeColorAndNotify(Guid materialId, ThemeColor color)
        {
            this.UpdateHoloGraphicAlbedo(materialId, this.currentTheme.GetColor(color));
            this.NotifyColorUpdate(color);
        }

        private void NotifyColorUpdate(ThemeColor color) =>
            this.ThemeUpdated?.Invoke(this, new ThemeUpdatedEventArgs
            {
                Theme = this.currentTheme,
                UpdatedColor = color,
            });

        private void UpdateFrontPlaneGradientTexture()
        {
            const int textureWidth = 256;
            const int textureHeight = 256;
            const int bytesPerPixel = 4;

            var texture = this.assetsService.Load<Texture>(CoreResourcesIDs.Textures.IridescentSpectrum);
            Span<byte> data = new byte[textureWidth * textureHeight * bytesPerPixel];

            var start = this.currentTheme.SecondaryColor4;
            var end = this.currentTheme.SecondaryColor5;
            var diff = new Vector4
            {
                X = end.R - start.R,
                Y = end.G - start.G,
                Z = end.B - start.B,
                W = end.A - start.A,
            };

            Color current;

            for (int i = 0; i < textureWidth; i++)
            {
                float factor = (float)i / textureWidth;
                current.R = (byte)(start.R + (factor * diff.X));
                current.G = (byte)(start.G + (factor * diff.Y));
                current.B = (byte)(start.B + (factor * diff.Z));
                current.A = (byte)(start.A + (factor * diff.W));

                for (int j = 0; j < textureHeight; j++)
                {
                    var pixelData = data.Slice(((j * textureWidth) + i) * bytesPerPixel, bytesPerPixel);
                    pixelData[0] = current.R;
                    pixelData[1] = current.G;
                    pixelData[2] = current.B;
                    pixelData[3] = current.A;
                }
            }

            this.graphicsContext.UpdateTextureData(texture, data.ToArray());
        }

        private void NotifyGlobalThemeUpdate()
        {
            if (this.notificationsEnabled)
            {
                this.ThemeUpdated?.Invoke(this, new ThemeUpdatedEventArgs
                {
                    Theme = this.CurrentTheme,
                });
            }
        }

        private void UpdateAllProximityLights()
        {
            var color = this.currentTheme.SecondaryColor4;
            var materials = new[]
            {
                CoreResourcesIDs.Materials.PrimaryColor1,
                CoreResourcesIDs.Materials.PrimaryColor2,
                CoreResourcesIDs.Materials.PrimaryColor3,
                CoreResourcesIDs.Materials.SecondaryColor1,
                CoreResourcesIDs.Materials.SecondaryColor2,
                CoreResourcesIDs.Materials.SecondaryColor3,
            };

            foreach (var material in materials)
            {
                this.UpdateProximityLight(material, color, this.frontPlateProximityLight);
            }

            foreach (var material in this.frontPlateMaterialIds)
            {
                this.UpdateProximityLight(material, color, this.frontPlateProximityLight);
            }

            this.UpdateProximityLight(
                MRTKResourceIDs.Materials.HolographicButtonContentCageProximity,
                color,
                this.buttonContentCageProximityLight);
        }

        private void UpdateProximityLight(Guid materialId, Color color, ProximityLightDiff diff)
        {
            var material = this.assetsService.Load<Material>(materialId);
            var holoGraphic = new HoloGraphic(material);
            holoGraphic.EnsureDirectiveIsActive(HoloGraphic.ProximityLightColorOverrideDirective, true);
            holoGraphic.ProximityLightCenterColorOverride = ProximityLightDiff.ApplyDiffTo(color, diff.CenterDiff);
            holoGraphic.ProximityLightMiddleColorOverride = ProximityLightDiff.ApplyDiffTo(color, diff.MiddleDiff);
            holoGraphic.ProximityLightOuterColorOverride = ProximityLightDiff.ApplyDiffTo(color, diff.OuterDiff);
        }

        private void UpdateAllMrtkMaterials() =>
            Enum.GetValues(typeof(ThemeColor))
                .Cast<ThemeColor>()
                .ToList()
                .ForEach(this.UpdateMrtkMaterialsByThemeColor);

        private void UpdateMrtkMaterialsByThemeColor(ThemeColor color)
        {
            var themedColor = this.currentTheme.GetColor(color);

            switch (color)
            {
                case ThemeColor.PrimaryColor2:
                    this.UpdateHoloGraphicAlbedo(MRTKResourceIDs.Materials.PinchSlider.PinchSlider_Thumb, themedColor);
                    this.UpdateHoloGraphicAlbedo(MRTKResourceIDs.Materials.Scrolling.ScrollViewerBar, themedColor);
                    break;
                case ThemeColor.PrimaryColor3:
                    this.UpdateHoloGraphicAlbedo(MRTKResourceIDs.Materials.loadingMat, themedColor);
                    this.UpdateHoloGraphicAlbedo(MRTKResourceIDs.Materials.PinchSlider.PinchSlider_Track, themedColor);
                    break;
            }
        }

        private class ProximityLightDiff
        {
            public Vector3 CenterDiff { get; set; }

            public Vector3 MiddleDiff { get; set; }

            public Vector3 OuterDiff { get; set; }

            public static Color ApplyDiffTo(Color color, Vector3 diff)
            {
                Func<byte, float, byte> minMax = (byte color, float diff) =>
                {
                    return (byte)Math.Min(byte.MaxValue, Math.Max(0, color + diff));
                };

                return new Color(minMax(color.R, diff.X), minMax(color.G, diff.Y), minMax(color.B, diff.Z));
            }
        }
    }
}
