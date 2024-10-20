﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.XR;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.XR;
using Evergine.Framework.XR.TrackedDevices;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.MRTK.Emulation;
using Evergine.Xrv.Core;
using System;

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

        [BindService]
        private XrvService xrvService = null;

        private Cursor cursor;
        private Vector3 lastPosition;
        private TimeSpan current;
        private TimeSpan betweenUpdate = TimeSpan.Zero;
        private float updateTime = 0.03f;
        private HoloGraphic pointerMaterial;
        private Transform3D pointerTransform;
        private XRTrackedDeviceType type;
        private XRHandedness hand;
        private CursorMaterialAssignation cursorMaterialAssignation;
        private bool wasDoingPichBefore;

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
        /// Gets or sets type.
        /// </summary>
        public XRTrackedDeviceType Type
        {
            get => this.type;
            set
            {
                this.type = value;
                if (this.IsAttached)
                {
                    this.UpdateHandTracking(subscribe: true);
                }
            }
        }

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
                    this.UpdateHandTracking(subscribe: true);
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
            this.UpdateHandTracking(subscribe: true);
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
            this.UpdateHandTracking(subscribe: false);
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            bool isPointerEnabled = this.cursor != null && this.cursor.Owner.IsEnabled;
            this.Pointer.IsEnabled = isPointerEnabled;

            if (!isPointerEnabled)
            {
                this.wasDoingPichBefore = false;
                this.current = TimeSpan.Zero;
                return;
            }

            this.current += gameTime;
            var position = this.pointerTransform.Position;

            // we need to update opacity for both paint and erase modes
            if (this.pointerMaterial != null)
            {
                this.pointerMaterial.Parameters_Alpha = this.cursor.Pinch ? 1f : this.MinAlpha;
            }

            // Pinch up
            if (this.manager.Mode != PainterModes.Painter && this.manager.Mode != PainterModes.Eraser)
            {
                this.wasDoingPichBefore = false;
                return;
            }

            /*
             * For some weird reason, it seems that we can't trust in cursor.PreviousPinch
             * as it's not always updated on time. I found a case that I was able to easily reproduce
             * (hiding one of the hands from device sensors) where that value remains as true
             * even if pinch gesture is physically stopped and done again. Maybe some kind of weird issue
             * with current implementation of rays and pinch update? Anyways, this is why we are storing
             * now our own value in wasDoingPichBefore field.
             */
            if (this.cursor.Pinch && !this.wasDoingPichBefore)
            {
                // Begin down
                this.manager.EndPaint(this.hand);
                this.DoAction(position);
                this.current = TimeSpan.Zero;
            }
            else if (this.cursor.Pinch && this.wasDoingPichBefore)
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
            else if (!this.cursor.Pinch && this.wasDoingPichBefore)
            {
                // Pinch up
                this.manager.EndPaint(this.hand);
            }

            this.wasDoingPichBefore = this.cursor.Pinch;
        }

        private void Manager_ModeChanged(object sender, PainterModes mode)
        {
            this.Pointer.IsEnabled = mode != PainterModes.Hand;
            this.pointerMaterial.Albedo = this.GetPointerColorByMode(mode);
            this.pointerTransform.LocalScale = mode == PainterModes.Eraser
                ? Vector3.One * RemoveCursorScaleFactor : Vector3.One;
        }

        private Color GetPointerColorByMode(PainterModes mode)
        {
            var theme = this.xrvService.ThemesSystem.CurrentTheme;
            return mode == PainterModes.Painter ? theme.PrimaryColor3 : theme.SecondaryColor3;
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

        private void UpdateHandTracking(bool subscribe)
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            if (this.cursor != null)
            {
                this.cursor.Owner.FindComponent<Transform3D>().PositionChanged -= this.PainterCursor_PositionChanged;
                this.cursor.Owner.AttachableStateChanged -= this.Owner_AttachableStateChanged;
            }

            if (!subscribe)
            {
                return;
            }

            var cursors = this.Owner.EntityManager.FindComponentsOfType<CursorTouch>(isExactType: false);
            foreach (var cursor in cursors)
            {
                TrackXRController trackXRController = Workarounds.GetControllerForCursor(cursor);
                if (trackXRController?.Handedness == this.Hand)
                {
                    if (trackXRController is TrackXRArticulatedHand)
                    {
                        if (this.type == XRTrackedDeviceType.Hand)
                        {
                            this.cursor = cursor;
                            break;
                        }
                    }
                    else
                    {
                        if (this.type == XRTrackedDeviceType.Controller)
                        {
                            this.cursor = cursor;
                            break;
                        }
                    }
                }
            }

            if (this.cursor != null)
            {
                this.cursor.Owner.FindComponent<Transform3D>().PositionChanged += this.PainterCursor_PositionChanged;
                this.cursor.Owner.AttachableStateChanged += this.Owner_AttachableStateChanged;
            }
        }

        private void Owner_AttachableStateChanged(object sender, AttachableObjectState state)
        {
            if (state == AttachableObjectState.Activated)
            {
                this.Pointer.IsEnabled = true;
            }

            if (state == AttachableObjectState.Deactivated)
            {
                this.Pointer.IsEnabled = false;
            }
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
            this.pointerMaterial.Albedo = this.GetPointerColorByMode(this.manager.Mode);
            this.pointerMaterial.Parameters_Alpha = this.MinAlpha;
        }
    }
}
