// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

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
    /// <summary>
    /// Audio note anchor handler.
    /// </summary>
    public class AudioNoteAnchorHandler : Component, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        /// <summary>
        /// Xrv service.
        /// </summary>
        [BindService]
        protected XrvService xrvService;

        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        /// <summary>
        /// Assets service.
        /// </summary>
        [BindComponent]
        protected Transform3D transform;

        /// <summary>
        /// Audio note anchor.
        /// </summary>
        [BindComponent]
        protected AudioNoteAnchor anchor;

        /// <summary>
        /// Tap detector.
        /// </summary>
        [BindComponent]
        protected TapDetector tapDetector;

        private Cursor currentCursor;
        private Vector3 initialOffset;
        private Vector3 lastCursorPosition;
        private bool touched;
        private Stopwatch clickWatch;
        private TimeSpan tapTime = TimeSpan.FromSeconds(0.4f);

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

            this.clickWatch = Stopwatch.StartNew();

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

        /// <inheritdoc/>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (eventData.EventHandled)
            {
                return;
            }

            if (this.clickWatch.Elapsed < this.tapTime)
            {
                return;
            }

            this.clickWatch.Stop();

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

                this.anchor.UpdateVisualState(this.touched ? AudioNoteAnchorVisual.Grabbed : AudioNoteAnchorVisual.Idle);
                eventData.SetHandled();
            }
        }

        /// <inheritdoc/>
        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            this.anchor.UpdateVisualState(this.anchor.IsSelected ? AudioNoteAnchorVisual.Selected : AudioNoteAnchorVisual.Idle);
            this.touched = false;
        }

        /// <inheritdoc/>
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.anchor.UpdateVisualState(AudioNoteAnchorVisual.Grabbed);
            this.touched = true;
        }

        /// <inheritdoc/>
        public void OnTouchUpdated(HandTrackingInputEventData eventData)
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

            this.tapDetector.OnTap += this.Handler_OnClick;
            return true;
        }

        private void Handler_OnClick(object sender, EventArgs e)
        {
            this.xrvService.PubSub.Publish(new AudioAnchorSelectedMessage()
            {
                Anchor = this.anchor,
            });
        }
    }
}
