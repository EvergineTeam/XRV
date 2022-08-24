using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using System;
using System.Diagnostics;

namespace Xrv.AudioNote
{
    public class TapDetector : Component, IMixedRealityTouchHandler
    {
        private Vector3 positionInit;
        private Stopwatch currentPressed;
        public float Tap { get; set; } = 0.5f;
        public float LongTap { get; set; } = 3f;
        public float MaxDistance { get; set; } = 0.1f;

        public event EventHandler OnTap;
        public event EventHandler OnLongTap;

        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            var distance = Vector3.Distance(eventData.Position, positionInit);
            if (distance < this.MaxDistance)
            {
                if (currentPressed.Elapsed < TimeSpan.FromSeconds(this.Tap))
                {
                    this.OnTap?.Invoke(this, EventArgs.Empty);
                }
                else if (currentPressed.Elapsed < TimeSpan.FromSeconds(this.LongTap))
                {
                    this.OnLongTap?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            this.positionInit = eventData.Position;
            currentPressed = Stopwatch.StartNew();
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData)
        {
        }
    }
}
