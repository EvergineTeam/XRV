using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Emulation;
using System;

namespace Xrv.AudioNote
{
    public class AudioNoteAnchorHandler : Component, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        [BindService]
        protected AssetsService assetsService = null;

        [BindComponent]
        protected Transform3D transform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Grab")]
        protected MaterialComponent materialComponent = null;

        protected Cursor currentCursor;
        protected Vector3 initialOffset;
        protected Vector3 lastCursorPosition;

        protected Material idle;
        protected Material grabbed;
        protected Material selected;

        protected bool touched;

        public event EventHandler OnClick;

        protected override bool OnAttached()
        {
            var result = base.OnAttached();

            this.idle = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.HandleIdle);
            this.selected = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.HandleSelected);
            this.grabbed = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.HandleGrabbed);

            return result;
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            this.OnClick.Invoke(this, EventArgs.Empty);
        }

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

        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            this.materialComponent.Material = this.idle;
            this.touched = false;
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.materialComponent.Material = this.selected;
            this.touched = true;
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }
    }
}
