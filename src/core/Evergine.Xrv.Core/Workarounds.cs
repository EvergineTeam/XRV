// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Evergine.Xrv.Core
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
            var toggle = button.FindComponentInChildren<ToggleButton>();
            ChangeToggleButtonState(toggle, setOn);
        }

        /// <summary>
        /// Change toggle button state to a given <see cref="ToggleButton.IsOn"/> status.
        /// </summary>
        /// <param name="toggle">Target toggle button.</param>
        /// <param name="setOn">Value indicating its status.</param>
        public static void ChangeToggleButtonState(ToggleButton toggle, bool setOn)
        {
            var toggleStateManager = toggle.Owner.FindComponentInChildren<ToggleStateManager>();
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
    }
}
