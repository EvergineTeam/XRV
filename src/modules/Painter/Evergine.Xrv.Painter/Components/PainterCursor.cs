// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.XR;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.MRTK.Emulation;
using System;
using System.Linq;

namespace Evergine.Xrv.Painter.Components
{
    /// <summary>
    /// Moves and update position for painter cursor.
    /// </summary>
    [AllowMultipleInstances]
    public class PainterCursor : Behavior
    {
        internal const int RemoveCursorScaleFactor = 2;

        [BindComponent]
        private PainterManager manager = null;

        private Cursor cursor;
        private Vector3 lastPosition;
        private TimeSpan current;
        private TimeSpan betweenUpdate = TimeSpan.Zero;
        private float updateTime = 0.03f;
        private HoloGraphic pointerMaterial;
        private Transform3D pointerTransform;
        private XRHandedness hand;
        private CursorMaterialAssignation cursorMaterialAssignation;

        /// <summary>
        /// Gets or sets update time.
        /// </summary>
        public float UpdateTime
        {
            get => this.updateTime;
            set
            {
                this.updateTime = value;
                this.betweenUpdate = TimeSpan.FromSeconds(this.updateTime);
            }
        }

        /// <summary>
        /// Gets or sets pointer.
        /// </summary>
        [IgnoreEvergine]
        public Entity Pointer { get; set; }

        /// <summary>
        /// Gets or sets hand.
        /// </summary>
        public XRHandedness Hand
        {
            get => this.hand;
            set
            {
                this.hand = value;
                if (this.IsAttached)
                {
                    this.UpdateHandTracking(value, true);
                }
            }
        }

        /// <summary>
        /// Gets or sets min Alpha.
        /// </summary>
        public float MinAlpha { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets min Alpha.
        /// </summary>
        public float PositionDelta { get; set; } = 0.005f;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            if (Application.Current.IsEditor)
            {
                return true;
            }

            this.Pointer.IsEnabled = false;
            this.pointerTransform = this.Pointer.FindComponent<Transform3D>();
            this.cursorMaterialAssignation = this.Pointer.FindComponentInChildren<CursorMaterialAssignation>();
            if (this.cursorMaterialAssignation != null)
            {
                this.cursorMaterialAssignation.MaterialUpdated += this.CursorMaterialAssignation_MaterialUpdated;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();

            if (this.cursorMaterialAssignation != null)
            {
                this.cursorMaterialAssignation.MaterialUpdated -= this.CursorMaterialAssignation_MaterialUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            if (Application.Current.IsEditor)
            {
                return;
            }

            var mode = this.manager.Mode;
            this.Pointer.IsEnabled = mode == PainterModes.Hand ? false : true;
            this.manager.ModeChanged += this.Manager_ModeChanged;
            this.UpdateHandTracking(this.hand, true);
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            if (Application.Current.IsEditor)
            {
                return;
            }

            this.Pointer.IsEnabled = false;

            this.manager.ModeChanged -= this.Manager_ModeChanged;
            this.UpdateHandTracking(this.hand, false);
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.cursor != null && this.cursor.Owner.IsEnabled)
            {
                this.current += gameTime;
                var position = this.pointerTransform.Position;

                if (this.pointerMaterial != null)
                {
                    this.pointerMaterial.Parameters_Alpha = this.cursor.Pinch ? 1f : this.MinAlpha;
                }

                if (this.cursor.Pinch && !this.cursor.PreviousPinch)
                {
                    // Begin down
                    this.DoAction(position);
                    this.current = TimeSpan.Zero;
                }
                else if (this.cursor.Pinch && this.cursor.PreviousPinch)
                {
                    // Pinch drag
                    if (this.current > this.betweenUpdate)
                    {
                        if (Vector3.Distance(this.lastPosition, position) > this.PositionDelta)
                        {
                            this.DoAction(position);
                        }

                        this.current = TimeSpan.Zero;
                    }
                }
                else if (!this.cursor.Pinch && this.cursor.PreviousPinch)
                {
                    // Pinch up
                    if (this.manager.Mode == PainterModes.Painter)
                    {
                        this.manager.EndPaint(this.hand);
                    }
                }
            }
            else
            {
                if (this.Pointer.IsEnabled)
                {
                    this.Pointer.IsEnabled = false;
                }
            }
        }

        private void Manager_ModeChanged(object sender, PainterModes mode)
        {
            this.Pointer.IsEnabled = mode != PainterModes.Hand;
            this.pointerMaterial.Albedo = mode == PainterModes.Painter ? Color.White : Color.Red;
            this.pointerTransform.LocalScale = mode == PainterModes.Eraser
                ? Vector3.One * RemoveCursorScaleFactor : Vector3.One;
        }

        private void DoAction(Vector3 position)
        {
            var mode = this.manager.Mode;
            if (mode == PainterModes.Painter)
            {
                this.manager.DoPaint(position, this.hand);
            }
            else if (mode == PainterModes.Eraser)
            {
                this.manager.DoErase(position);
            }

            this.lastPosition = position;
        }

        private void UpdateHandTracking(XRHandedness value, bool subscribe)
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            if (this.cursor != null)
            {
                this.cursor.Owner.FindComponent<Transform3D>().PositionChanged -= this.PainterCursor_PositionChanged;
            }

            if (!subscribe)
            {
                return;
            }

            this.cursor = this.Owner.EntityManager.FindComponentsOfType<CursorTouch>(isExactType: false).FirstOrDefault(c => c.Owner.Name.Contains(value.ToString()));
            this.cursor.Owner.FindComponent<Transform3D>().PositionChanged += this.PainterCursor_PositionChanged;
        }

        private void PainterCursor_PositionChanged(object sender, EventArgs e)
        {
            if (sender is Transform3D transform)
            {
                this.pointerTransform.Position = transform.Position;
            }
        }

        private void CursorMaterialAssignation_MaterialUpdated(object sender, EventArgs e)
        {
            this.pointerMaterial = new HoloGraphic(this.Pointer.FindComponentInChildren<MaterialComponent>().Material);
            this.pointerMaterial.Albedo = this.manager.Mode == PainterModes.Painter ? Color.White : Color.Red;
            this.pointerMaterial.Parameters_Alpha = this.MinAlpha;
        }
    }
}
