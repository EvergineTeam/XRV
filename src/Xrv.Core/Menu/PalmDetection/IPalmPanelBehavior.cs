using Evergine.Framework;
using Evergine.Framework.XR;
using System;

namespace Xrv.Core.Menu
{
    public interface IPalmPanelBehavior
    {
        XRHandedness ActiveHandedness { get; }

        float DistanceFromHand { get; set; }

        XRHandedness Handedness { get; set; }

        bool IsPalmUp { get; }

        float LookAtCameraLowerThreshold { get; set; }

        float LookAtCameraUpperThreshold { get; set; }

        float OpenPalmLowerThreshold { get; set; }

        float OpenPalmUpperThreshold { get; set; }

        Entity Owner { get; }

        float TimeBetweenActivationChanges { get; set; }

        event EventHandler<XRHandedness> ActiveHandednessChanged;

        event EventHandler<bool> PalmUpChanged;
    }
}
