﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.XR;
using Evergine.Framework;
using Evergine.MRTK.Emulation;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;

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
            toggle.IsOn = setOn;
        }

        /// <summary>
        /// Gets controller associated to a <see cref="Cursor"/>.
        /// </summary>
        /// <param name="cursor">Target cursor.</param>
        /// <returns>Associated controller.</returns>
        public static TrackXRController GetControllerForCursor(Cursor cursor)
        {
            var root = cursor.Owner.Parent.Parent;
            TrackXRController controller = root.FindComponentInChildren<TrackXRController>(isExactType: false);
            return controller;
        }
    }
}
