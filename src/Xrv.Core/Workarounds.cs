// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Xrv.Core
{
    /// <summary>
    /// A place to centralize some workarounds, that we need to fix in MRTK or engine.
    /// </summary>
    public static class Workarounds
    {
        /// <summary>
        /// When assigning a instance of button configurator with a null back plate, it's
        /// not applied. Probably we need to check how MRTK applies configurations.
        /// </summary>
        /// <param name="button">Target button entity.</param>
        /// <param name="tag">Tag to take material component from.</param>
        public static void MrtkForceButtonNullPlate(Entity button, string tag = "PART_Plate")
        {
            // Workaround MTRK ignores null material on StandardButtonConfigurator
            button.FindChildrenByTag(tag, true).First().FindComponent<MaterialComponent>().Material = null;
        }

        /// <summary>
        /// MRTK buttons look to negative Z, so we have to invert this component.
        /// </summary>
        /// <param name="button">Button entity.</param>
        public static void MrtkRotateButton(Entity button)
        {
            var buttonTransform = button.FindComponent<Transform3D>();
            var rotation = buttonTransform.LocalRotation;
            rotation.Y = MathHelper.Pi;
            buttonTransform.LocalRotation = rotation;
        }

        /// <summary>
        /// Update toggle button state programatically.
        /// </summary>
        /// <param name="button">Target button entity.</param>
        /// <param name="setOn">Toggle status.</param>
        public static void ChangeToggleButtonState(Entity button, bool setOn)
        {
            var toggle = button.FindComponentInChildren<ToggleButton>();
            if (toggle.IsOn != setOn)
            {
                var pressableButton = button.FindComponentInChildren<PressableButton>();
                pressableButton.SimulatePress();
            }
        }
    }
}
