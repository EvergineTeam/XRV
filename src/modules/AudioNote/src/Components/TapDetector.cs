// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using System;
using System.Diagnostics;

namespace Xrv.AudioNote
{
    // TODO move to core?

    /// <summary>
    /// Tap / Click detector component.
    /// </summary>
    public class TapDetector : Component, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        private Vector3 positionInit;
        private Stopwatch clickWatch;
        private TimeSpan tapTime;
        private TimeSpan mintouchTime;
        private Stopwatch touchWatch;

        /// <summary>
        /// Tap.
        /// </summary>
        public event EventHandler OnTap;

        /// <summary>
        /// Tap.
        /// </summary>
        public event EventHandler OnLongTap;

        /// <summary>
        /// Gets or sets tap.
        /// </summary>
        public float Tap { get; set; } = 0.5f;

        /// <summary>
        /// Gets or sets tap.
        /// </summary>
        public float LongTap { get; set; } = 3f;

        /// <summary>
        /// Gets or sets tap.
        /// </summary>
        public float MaxDistance { get; set; } = 0.1f;

        /// <inheritdoc/>
        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            this.touchWatch.Stop();
            if (this.touchWatch.Elapsed > this.mintouchTime // avoid touch by mistake
                && this.touchWatch.Elapsed < this.tapTime)
            {
                this.OnTap?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.touchWatch = Stopwatch.StartNew();
        }

        /// <inheritdoc/>
        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }

        /// <inheritdoc/>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            this.positionInit = eventData.Position;
            this.clickWatch = Stopwatch.StartNew();
        }

        /// <inheritdoc/>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            this.clickWatch.Stop();
            var distance = Vector3.Distance(eventData.Position, this.positionInit);
            if (distance < this.MaxDistance)
            {
                if (this.clickWatch.Elapsed < TimeSpan.FromSeconds(this.Tap))
                {
                    this.OnTap?.Invoke(this, EventArgs.Empty);
                }
                else if (this.clickWatch.Elapsed < TimeSpan.FromSeconds(this.LongTap))
                {
                    this.OnLongTap?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.tapTime = TimeSpan.FromSeconds(this.Tap);
            this.mintouchTime = TimeSpan.FromSeconds(0.2f);

            return true;
        }
    }
}
