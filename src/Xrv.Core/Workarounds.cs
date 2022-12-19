// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Networking.Components;
using System;
using System.Linq;

namespace Xrv.Core
{
    /// <summary>
    /// A place to centralize some workarounds, that we need to fix in MRTK or engine.
    /// </summary>
    public static class Workarounds
    {
        /// <summary>
        /// Update toggle button state programatically.
        /// </summary>
        /// <param name="button">Target button entity.</param>
        /// <param name="setOn">Toggle status.</param>
        public static void ChangeToggleButtonState(Entity button, bool setOn)
        {
            var toggleStateManager = button.FindComponentInChildren<ToggleStateManager>();
            var toggleStates = toggleStateManager.States;
            if (toggleStates != null)
            {
                var newToggleState = setOn ? ToggleState.On : ToggleState.Off;
                var newState = toggleStates.Where(s => s.Value == newToggleState).FirstOrDefault();

                if (newState != null)
                {
                    toggleStateManager.ChangeState(newState);
                }
            }
        }

        /// <summary>
        /// Forces networking property refresh (so, it's synchronized in rest of clients)
        /// </summary>
        /// <typeparam name="K">The type of the property key.Must be System.Byte or System.Enum.</typeparam>
        /// <typeparam name="V">The type of the property value.</typeparam>
        /// <param name="property">Network property.</param>
        public static void ForceRefresh<K, V>(this NetworkPropertySync<K, V> property)
            where K : struct, IConvertible
        {
            // TODO: create ForceRefresh method in Evergine.Networking
            property.PropertyValue = property.PropertyValue;
        }
    }
}
