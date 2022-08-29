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

namespace Xrv.Ruler
{
    public class RulerBehavior : Behavior
    {
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle1")]
        protected Transform3D handle1Transform;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle2")]
        protected Transform3D handle2Transform;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Line")]
        protected LineMesh line;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_Label")]
        protected Transform3D labelTransform;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelNumber")]
        protected Text3DMesh labelNumber;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_labelUnits")]
        protected Text3DMesh labelUnits;

        public enum MeasureUnits
        {
            Meters,
            Feets,
        }

        protected Transform3D lineTransform;
        protected Entity settings;
        protected ToggleButton mettersToggle;
        protected ToggleButton feetsToggle;

        public MeasureUnits Units { get; set; }

        public Entity Settings
        {
            get => this.settings;
            set
            {
                this.settings = value;
                this.mettersToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Meters", skipOwner: true);
                this.feetsToggle = this.settings.FindComponentInChildren<ToggleButton>(tag: "PART_Feets", skipOwner: true);

                this.mettersToggle.Toggled += this.UnitChanged;
                this.feetsToggle.Toggled += this.UnitChanged;
            }
        }

        private void UnitChanged(object sender, EventArgs e)
        {
            if (this.mettersToggle.IsOn)
            {
                this.Units = MeasureUnits.Meters;
            }
            else if (this.feetsToggle.IsOn)
            {
                this.Units = MeasureUnits.Feets;
            }
        }
      
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
        }

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

        public void Reset()
        {
            var cameraTransform = this.Managers.RenderManager.ActiveCamera3D.Transform;
            var cameraWorldTransform = cameraTransform.WorldTransform;
            var center = cameraTransform.Position + cameraWorldTransform.Forward * 0.6f;
            this.handle1Transform.Position = center + cameraWorldTransform.Left * 0.5f;
            this.handle2Transform.Position = center + cameraWorldTransform.Right * 0.5f;
        }

        private void UpdateMeasureString(float distance)
        {

            string measurement;
            string unit;

            switch (this.Units)
            {
                case MeasureUnits.Feets:

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
    }
}