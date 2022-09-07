// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Effects;
using Evergine.MRTK.Emulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Moves and update position for painter cursor.
    /// </summary>
    public class PainterCursor : Component, IMixedRealityPointerHandler
    {
        /// <summary>
        /// Painter manager.
        /// </summary>
        [BindComponent]
        protected PainterManager manager;

        private IEnumerable<Cursor> cursors;
        private Vector3 lastPosition;
        private Stopwatch current;
        private TimeSpan betweenUpdate = TimeSpan.Zero;
        private float secondsBetweenUpdate = 1f;
        private HoloGraphic pointerMaterial;
        private Transform3D pointerTransform;


        /// <summary>
        /// Gets or sets tiks.
        /// </summary>
        public float SecondsBetweenUpdate
        {
            get => this.secondsBetweenUpdate;
            set
            {
                this.secondsBetweenUpdate = value;
                this.betweenUpdate = TimeSpan.FromSeconds(this.secondsBetweenUpdate);
            }
        }

        /// <summary>
        /// Gets or sets pointer.
        /// </summary>
        [IgnoreEvergine]
        public Entity Pointer { get; set; }

        /// <summary>
        /// Gets or sets min Alpha.
        /// </summary>
        public float MinAlpha { get; set; } = 0.5f;

        /// <inheritdoc/>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            this.current = Stopwatch.StartNew();
            this.DoAction(eventData);
            this.pointerMaterial.Parameters_Alpha = 1f;
        }

        /// <inheritdoc/>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (this.current.Elapsed > this.betweenUpdate)
            {
                this.current.Restart();
                if (Vector3.Distance(this.lastPosition, eventData.Position) > 0.01f)
                {
                    this.DoAction(eventData);
                }
            }
        }

        /// <inheritdoc/>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (this.manager.Mode == PainterModes.Painter)
            {
                this.manager.EndPaint();
            }

            this.pointerMaterial.Parameters_Alpha = this.MinAlpha;
            this.current.Stop();
        }

        /// <inheritdoc/>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

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

            this.cursors = this.Owner.EntityManager.FindComponentsOfType<CursorTouch>(isExactType: false);

            this.Pointer.IsEnabled = false;
            this.pointerMaterial = new HoloGraphic(this.Pointer.FindComponentInChildren<MaterialComponent>().Material);
            this.pointerTransform = this.Pointer.FindComponent<Transform3D>();

            return true;
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
            this.pointerMaterial.Albedo = mode == PainterModes.Painter ? Color.White : Color.Red;
            this.pointerMaterial.Parameters_Alpha = this.MinAlpha;
            foreach (var item in this.cursors)
            {
                item.Owner.FindComponent<Transform3D>().PositionChanged += this.PainterCursor_PositionChanged;
            }

            this.manager.OnModeChanged += this.Manager_OnModeChanged;
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
            foreach (var item in this.cursors)
            {
                item.Owner.FindComponent<Transform3D>().PositionChanged -= this.PainterCursor_PositionChanged;
            }

            this.manager.OnModeChanged -= this.Manager_OnModeChanged;
        }

        private void PainterCursor_PositionChanged(object sender, EventArgs e)
        {
            if (sender is Transform3D transform)
            {
                this.pointerTransform.Position = transform.Position;
            }
        }

        private void Manager_OnModeChanged(object sender, PainterModes e)
        {
            this.pointerMaterial.Albedo = e == PainterModes.Painter ? Color.White : Color.Red;
        }

        private void DoAction(MixedRealityPointerEventData eventData)
        {
            var mode = this.manager.Mode;
            if (mode == PainterModes.Painter)
            {
                this.manager.DoPaint(eventData.Position);
            }
            else if (mode == PainterModes.Eraser)
            {
                this.manager.DoErase(eventData.Position);
            }

            this.lastPosition = eventData.Position;
        }
    }
}
