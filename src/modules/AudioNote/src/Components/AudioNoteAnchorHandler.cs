using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Emulation;
using System;
using System.Diagnostics;
using Xrv.AudioNote.Messages;
using Xrv.Core;

namespace Xrv.AudioNote
{
    public class AudioNoteAnchorHandler : Component, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        [BindService]
        protected XrvService xrvService;

        [BindService]
        protected AssetsService assetsService = null;

        [BindComponent]
        protected Transform3D transform = null;

        protected Cursor currentCursor;
        protected Vector3 initialOffset;
        protected Vector3 lastCursorPosition;

        [BindComponent]
        protected AudioNoteAnchor anchor;

        [BindComponent]
        protected TapDetector tapDetector;

        protected bool touched;
        private Stopwatch watch;
        private TimeSpan tapTime = TimeSpan.FromSeconds(0.4f);


        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;


            this.tapDetector.OnTap += Handler_OnClick;
            return true;
        }


        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (eventData.EventHandled)
            {
                return;
            }

            this.watch = Stopwatch.StartNew();

            if (this.currentCursor == null)
            {
                if (eventData.CurrentTarget == this.Owner)
                {
                    this.currentCursor = eventData.Cursor;

                    this.initialOffset = this.transform.Position - eventData.Position;
                    this.transform.Position = eventData.Position + this.initialOffset;
                    this.lastCursorPosition = eventData.Position;
                    this.anchor.UpdateVisualState(AudioNoteAnchorVisual.Grabbed);

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

            if (this.watch.Elapsed < this.tapTime) return;
            this.watch.Stop();

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
                eventData.SetHandled();
            }
        }

        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            this.anchor.UpdateVisualState(this.anchor.IsSelected? AudioNoteAnchorVisual.Selected : AudioNoteAnchorVisual.Idle);
            this.touched = false;
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.anchor.UpdateVisualState(AudioNoteAnchorVisual.Selected);
            this.touched = true;
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }


        private void Handler_OnClick(object sender, EventArgs e)
        {
            this.xrvService.PubSub.Publish(new AudioAnchorSelected()
            {
                Anchor = this.anchor,
            });
        }
    }
}
