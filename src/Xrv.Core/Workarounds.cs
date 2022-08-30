﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Xrv.Core
{
    public static class Workarounds
    {
        public static void MrtkForceButtonNullPlate(Entity button, string tag = "PART_Plate")
        {
            // Workaround MTRK ignores null material on StandardButtonConfigurator
            button.FindChildrenByTag(tag, true).First().FindComponent<MaterialComponent>().Material = null;
        }

        public static void MrtkRotateButton(Entity button)
        {
            // MRTK buttons look to negative Z, so we have to invert this component
            var buttonTransform = button.FindComponent<Transform3D>();
            var rotation = buttonTransform.LocalRotation;
            rotation.Y = MathHelper.Pi;
            buttonTransform.LocalRotation = rotation;
        }

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
