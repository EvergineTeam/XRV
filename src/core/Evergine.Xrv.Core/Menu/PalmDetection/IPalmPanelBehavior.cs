// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.XR;
using System;

namespace Evergine.Xrv.Core.Menu
{
    /// <summary>
    /// Palm twist behavior to notify when palm is detected and orientation changes.
    /// </summary>
    public interface IPalmPanelBehavior
    {
        /// <summary>
        /// Raised when active hand has changed.
        /// </summary>
        event EventHandler<XRHandedness> ActiveHandednessChanged;

        /// <summary>
        /// Raised when activa hand palm is twisted.
        /// </summary>
        event EventHandler<bool> PalmUpChanged;

        /// <summary>
        /// Gets active hand.
        /// </summary>
        XRHandedness ActiveHandedness { get; }

        /// <summary>
        /// Gets or sets distance from the hand to the entity.
        /// </summary>
        float DistanceFromHand { get; set; }

        /// <summary>
        /// Gets or sets an explicit hand to be considered when detecting palm twist.
        /// </summary>
        XRHandedness Handedness { get; set; }

        /// <summary>
        /// Gets a value indicating whether active palm is up.
        /// </summary>
        bool IsPalmUp { get; }

        /// <summary>
        /// Gets or sets amount that represents how much the palm has to be looking away from the camera to consider it as down.
        /// </summary>
        float LookAtCameraLowerThreshold { get; set; }

        /// <summary>
        /// Gets or sets amount that represents how much the palm has to be looking to the camera to consider it as up.
        /// </summary>
        float LookAtCameraUpperThreshold { get; set; }

        /// <summary>
        /// Gets or sets amount that represents how much the hand has to be open to consider the palm as down.
        /// </summary>
        float OpenPalmLowerThreshold { get; set; }

        /// <summary>
        /// Gets or sets amount that represents how much the hand has to be open to consider the palm as up.
        /// </summary>
        float OpenPalmUpperThreshold { get; set; }

        /// <summary>
        /// Gets menu owner.
        /// </summary>
        Entity Owner { get; }

        /// <summary>
        /// Gets or sets Minimum amount of time in seconds between two consecutive activation changes.
        /// </summary>
        float TimeBetweenActivationChanges { get; set; }
    }
}
