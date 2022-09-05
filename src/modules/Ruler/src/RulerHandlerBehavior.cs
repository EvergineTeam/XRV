// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Emulation;

namespace Xrv.Ruler
{
    /// <summary>
    /// Manage the ruler handlers.
    /// </summary>
    public class RulerHandlerBehavior : Component, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        private Cursor currentCursor;
        private Vector3 initialOffset;
        private Vector3 lastCursorPosition;

        private Material idle;
        private Material grabbed;
        private Material selected;

        private bool touched;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent]
        private MaterialComponent materialComponent = null;

        /// <inheritdoc/>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.EventHandled)
            {
                return;
            }

            if (this.currentCursor == null)
            {
                if (eventData.CurrentTarget == this.Owner)
                {
                    this.currentCursor = eventData.Cursor;

                    this.initialOffset = this.transform.Position - eventData.Position;
                    this.transform.Position = eventData.Position + this.initialOffset;
                    this.lastCursorPosition = eventData.Position;
                    this.materialComponent.Material = this.grabbed;

                    eventData.SetHandled();
                }
            }
        }

        /// <inheritdoc/>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (eventData.EventHandled)
            {
                return;
            }

            if (this.currentCursor == eventData.Cursor)
            {
                Vector3 delta = eventData.Position - this.lastCursorPosition;

                this.transform.Position = this.lastCursorPosition + delta + this.initialOffset;
                this.lastCursorPosition = this.lastCursorPosition + delta;

                eventData.SetHandled();
            }
        }

        /// <inheritdoc/>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (eventData.EventHandled)
            {
                return;
            }

            if (this.currentCursor == eventData.Cursor)
            {
                this.currentCursor = null;
                this.materialComponent.Material = this.touched ? this.selected : this.idle;
                eventData.SetHandled();
            }
        }

        /// <inheritdoc/>
        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            this.materialComponent.Material = this.idle;
            this.touched = false;
        }

        /// <inheritdoc/>
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.materialComponent.Material = this.selected;
            this.touched = true;
        }

        /// <inheritdoc/>
        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            var result = base.OnAttached();

            this.idle = this.assetsService.Load<Material>(RulerResourceIDs.Materials.HandleIdle);
            this.selected = this.assetsService.Load<Material>(RulerResourceIDs.Materials.HandleSelected);
            this.grabbed = this.assetsService.Load<Material>(RulerResourceIDs.Materials.HandleGrabbed);

            return result;
        }
    }
}
