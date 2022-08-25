using Evergine.Framework;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using System;
using System.Diagnostics;

namespace Xrv.AudioNote
{
    // TODO move to core?
    public class TapDetector : Component, IMixedRealityPointerHandler
    {
        private Vector3 positionInit;
        private Stopwatch currentPressed;
        public float Tap { get; set; } = 0.5f;
        public float LongTap { get; set; } = 3f;
        public float MaxDistance { get; set; } = 0.1f;

        public event EventHandler OnTap;
        public event EventHandler OnLongTap;

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            this.positionInit = eventData.Position;
            this.currentPressed = Stopwatch.StartNew();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            this.currentPressed.Stop();
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
    }
}
