// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Fonts;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Themes;

namespace Evergine.Xrv.Ruler
{
    /// <summary>
    /// Behavior to control ruler handles and update measures.
    /// </summary>
    public class RulerBehavior : Behavior
    {
        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_ruler_handle1")]
        private Entity handle1Entity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_ruler_handle2")]
        private Entity handle2Entity = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Line")]
        private LineMesh line = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Label")]
        private Transform3D labelTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelNumber")]
        private Text3DMesh labelNumber = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelUnits")]
        private Text3DMesh labelUnits = null;

        private Transform3D handle1Transform = null;
        private Transform3D handle2Transform = null;
        private Transform3D lineTransform;
        private Entity settings;
        private ToggleButton mettersToggle;
        private ToggleButton feetToggle;

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
        [IgnoreEvergine]
        public Entity Settings
        {
            get => this.settings;
            set
            {
                this.settings = value;
                this.mettersToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Meters");
                this.feetToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Feet");

                this.mettersToggle.Toggled += this.UnitChanged;
                this.feetToggle.Toggled += this.UnitChanged;
            }
        }

        /// <summary>
        /// Resets ruler.
        /// </summary>
        public void Reset()
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

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

            this.handle1Transform = this.handle1Transform ?? this.handle1Entity.FindComponentInChildren<RulerHandlerBehavior>(isRecursive: true).HandleTransform;
            this.handle2Transform = this.handle2Transform ?? this.handle2Entity.FindComponentInChildren<RulerHandlerBehavior>(isRecursive: true).HandleTransform;

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
                    point.Color = theme.PrimaryColor3;
                }

                this.line.Refresh();
            }

            this.labelNumber.Color = theme.PrimaryColor3;
        }

        private void OnSecondaryColor1Updated(Theme theme)
        {
            this.labelUnits.Color = theme.SecondaryColor1;
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
