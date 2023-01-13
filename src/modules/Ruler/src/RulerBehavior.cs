// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Components.Fonts;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Collections.Generic;
using Xrv.Core;
using Xrv.Core.Themes;

namespace Xrv.Ruler
{
    /// <summary>
    /// Behavior to control ruler handles and update measures.
    /// </summary>
    public class RulerBehavior : Behavior
    {
        /// <summary>
        /// Transform for first handle.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle1")]
        protected Transform3D handle1Transform;

        /// <summary>
        /// Transform for second handle.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle2")]
        protected Transform3D handle2Transform;

        /// <summary>
        /// Line mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Line")]
        protected LineMesh line;

        /// <summary>
        /// Label transform.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Label")]
        protected Transform3D labelTransform;

        /// <summary>
        /// Label text.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelNumber")]
        protected Text3DMesh labelNumber;

        /// <summary>
        /// Units text.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelUnits")]
        protected Text3DMesh labelUnits;

        /// <summary>
        /// Line transform.
        /// </summary>
        protected Transform3D lineTransform;

        /// <summary>
        /// Settings entity.
        /// </summary>
        protected Entity settings;

        /// <summary>
        /// Toggle for metters.
        /// </summary>
        protected ToggleButton mettersToggle;

        /// <summary>
        /// Toggle for feet.
        /// </summary>
        protected ToggleButton feetToggle;

        [BindService]
        private XrvService xrvService = null;

        /// <summary>
        /// Measure units.
        /// </summary>
        public enum MeasureUnits
        {
            /// <summary>
            /// Meters.
            /// </summary>
            Meters,

            /// <summary>
            /// Feet.
            /// </summary>
            Feet,
        }

        /// <summary>
        /// Gets or sets measure units.
        /// </summary>
        public MeasureUnits Units { get; set; }

        /// <summary>
        /// Gets or sets settings entity.
        /// </summary>
        public Entity Settings
        {
            get => this.settings;
            set
            {
                this.settings = value;
                this.mettersToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Meters", skipOwner: true);
                this.feetToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Feets", skipOwner: true);

                this.mettersToggle.Toggled += this.UnitChanged;
                this.feetToggle.Toggled += this.UnitChanged;
            }
        }

        /// <summary>
        /// Resets ruler.
        /// </summary>
        public void Reset()
        {
            var cameraTransform = this.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            var center = cameraTransform.Position + (cameraWorldTransform.Forward * 0.6f);
            this.handle1Transform.Position = center + (cameraWorldTransform.Left * 0.5f);
            this.handle2Transform.Position = center + (cameraWorldTransform.Right * 0.5f);
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.lineTransform == null)
            {
                this.lineTransform = this.line.Owner.FindComponent<Transform3D>();
                const float thickness = 0.012f;
                this.line.LinePoints = new List<LinePointInfo>()
                {
                    new LinePointInfo() { Position = Vector3.Backward * 0.5f, Thickness = thickness, Color = Color.White },
                    new LinePointInfo() { Position = Vector3.Forward * 0.5f, Thickness = thickness, Color = Color.White },
                };
                this.line.Refresh();
            }

            this.Reset();

            if (this.xrvService?.ThemesSystem != null)
            {
                this.RefreshThemeDependantElements(this.xrvService.ThemesSystem.CurrentTheme);
                this.xrvService.ThemesSystem.ThemeUpdated += this.ThemesSystem_ThemeUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.xrvService?.ThemesSystem != null)
            {
                this.RefreshThemeDependantElements(this.xrvService.ThemesSystem.CurrentTheme);
                this.xrvService.ThemesSystem.ThemeUpdated -= this.ThemesSystem_ThemeUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            // Calculate distance and middlepoint
            var fromPosition = this.handle1Transform.Position;
            var toPosition = this.handle2Transform.Position;
            var direction = toPosition - fromPosition;
            var middlePoint = fromPosition + (direction * 0.5f);
            var distance = direction.Length();

            // Update ruler line
            this.lineTransform.Position = middlePoint;
            var scale = this.lineTransform.Scale;
            scale.Z = distance;
            this.lineTransform.Scale = scale;
            this.lineTransform.LookAt(toPosition, Vector3.Up);

            // Update ruler label
            this.labelTransform.Position = middlePoint;
            this.labelTransform.LocalLookAt(toPosition, Vector3.Up);
            this.UpdateMeasureString(distance);
        }

        private void UnitChanged(object sender, EventArgs e)
        {
            if (this.mettersToggle.IsOn)
            {
                this.Units = MeasureUnits.Meters;
            }
            else if (this.feetToggle.IsOn)
            {
                this.Units = MeasureUnits.Feet;
            }
        }

        private void UpdateMeasureString(float distance)
        {
            string measurement;
            string unit;

            switch (this.Units)
            {
                case MeasureUnits.Feet:

                    measurement = $"{Math.Round(distance * 3.280839895, 2):##.00}";
                    unit = "ft";

                    break;
                case MeasureUnits.Meters:
                default:
                    var linearDistanceAbs = Math.Abs(distance);
                    if (linearDistanceAbs < 1)
                    {
                        measurement = $"{(int)(distance * 1000.0f)}";
                        unit = "mm";
                    }
                    else if (linearDistanceAbs < 100)
                    {
                        measurement = $"{Math.Round(distance, 2):##.00}";
                        unit = "m";
                    }
                    else if (linearDistanceAbs < 1000)
                    {
                        measurement = $"{Math.Round(distance, 1)}";
                        unit = "m";
                    }
                    else
                    {
                        measurement = $"{(int)Math.Truncate(distance)}";
                        unit = "m";
                    }

                    break;
            }

            this.labelNumber.Text = measurement;
            this.labelUnits.Text = unit;
        }

        private void RefreshThemeDependantElements(Theme theme)
        {
            this.OnPrimaryColor3Updated(theme);
            this.OnSecondaryColor1Updated(theme);
        }

        private void OnPrimaryColor3Updated(Theme theme)
        {
            if (this.line != null)
            {
                foreach (var point in this.line.LinePoints)
                {
                    point.Color = theme[ThemeColor.PrimaryColor3];
                }

                this.line.Refresh();
            }

            this.labelNumber.Color = theme[ThemeColor.PrimaryColor3];
        }

        private void OnSecondaryColor1Updated(Theme theme)
        {
            this.labelUnits.Color = theme[ThemeColor.SecondaryColor1];
        }

        private void ThemesSystem_ThemeUpdated(object sender, ThemeUpdatedEventArgs args)
        {
            if (args.IsNewThemeInstance)
            {
                this.RefreshThemeDependantElements(args.Theme);
                return;
            }

            switch (args.UpdatedColor)
            {
                case ThemeColor.PrimaryColor3:
                    this.OnPrimaryColor3Updated(args.Theme);
                    break;
                case ThemeColor.SecondaryColor1:
                    this.OnSecondaryColor1Updated(args.Theme);
                    break;
                default:
                    break;
            }
        }
    }
}
