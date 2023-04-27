// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.XR;
using System;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// <see cref="TrackedElement"/> extension methods.
    /// </summary>
    public static class TrackedElementExtensions
    {
        /// <summary>
        /// Determines if a tracked element is handedness.
        /// </summary>
        /// <param name="element">Tracked element.</param>
        /// <returns>True if handedness; false otherwise.</returns>
        public static bool IsHandedness(this TrackedElement element)
        {
            switch (element)
            {
                case TrackedElement.LeftHand:
                case TrackedElement.LeftController:
                case TrackedElement.RightHand:
                case TrackedElement.RightController:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets <see cref="XRHandedness"/> value for a tracked element.
        /// </summary>
        /// <param name="element">Tracked element.</param>
        /// <returns>Handedness value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Raised when asked for
        /// a non-handedness element, like <see cref="TrackedElement.Head"/>.</exception>
        public static XRHandedness GetHandedness(this TrackedElement element)
        {
            switch (element)
            {
                case TrackedElement.LeftHand:
                case TrackedElement.LeftController:
                    return XRHandedness.LeftHand;
                case TrackedElement.RightHand:
                case TrackedElement.RightController:
                    return XRHandedness.RightHand;
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }

        /// <summary>
        /// Determines if tracked element is a hand.
        /// </summary>
        /// <param name="element">Tracked element.</param>
        /// <returns>True if it's a hand; false otherwise.</returns>
        public static bool IsHand(this TrackedElement element) =>
            element == TrackedElement.LeftHand || element == TrackedElement.RightHand;

        /// <summary>
        /// Determines if tracked element is a controller.
        /// </summary>
        /// <param name="element">Tracked element.</param>
        /// <returns>True if it's a controller; false otherwise.</returns>
        public static bool IsController(this TrackedElement element) =>
            element == TrackedElement.LeftController || element == TrackedElement.RightController;
    }
}
