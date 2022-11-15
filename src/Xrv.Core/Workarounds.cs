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
            // TODO: this has been fixed in the MRTK (commit 714737b)
            // When the MRTK package has been updated to a version that includes this change, replace every call
            // to Workarounds.MrtkForceButtonNullPlate to setting AllowBackPlateNullMaterial to true in
            // the corresponding ButtonConfigurator

            // Workaround MTRK ignores null material on StandardButtonConfigurator
            button.FindChildrenByTag(tag, true).First().FindComponent<MaterialComponent>().Material = null;
        }

        /// <summary>
        /// MRTK buttons look to negative Z, so we have to invert this component.
        /// </summary>
        /// <param name="button">Button entity.</param>
        public static void MrtkRotateButton(Entity button)
        {
            // TODO: this has been fixed in the MRTK (commit dcadcdb)
            // When the MRTK package has been updated to a version that includes this change, delete every call
            // to Workarounds.MrtkRotateButton. If there are any existing prefabs in the XRV using the old convention,
            // they will have to be manually rotated in the editor and regenerated.
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
            // TODO: This is not really a bug in the MRTK. However, improved support for simulated inputs is planned for the
            // input system update to make it more consistent.
            // There is another way of changing the state of the ToggleButton that is displayed here. However, this requires
            // for the PubSubOnButtonPress<TMessage> component to subscribe to the ToggleButton's Toggled event
            // instead of the PressableButton's ButtonReleased event for ToggleButtons, although it can use the same event handler.
            // Doing this will make the button not emit a sound when the state is changed since the audio feedback component
            // subscribes to the ButtonPressed and ButtonReleased events.
            // It is also recommended to move this method to a Utils class.

            ////var toggleStateManager = button.FindComponentInChildren<ToggleStateManager>();
            ////var toggleStates = toggleStateManager.States;
            ////if (toggleStates != null)
            ////{
            ////    var newToggleState = setOn ? ToggleState.On : ToggleState.Off;
            ////    var newState = toggleStates.Where(s => s.Value == newToggleState).FirstOrDefault();

            ////    if (newState != null)
            ////    {
            ////        toggleStateManager.ChangeState(newState);
            ////    }
            ////}

            var toggle = button.FindComponentInChildren<ToggleButton>();
            if (toggle.IsOn != setOn)
            {
                var pressableButton = button.FindComponentInChildren<PressableButton>();
                pressableButton.SimulatePress();
            }
        }
    }
}
