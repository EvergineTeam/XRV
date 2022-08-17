using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using System.Linq;

namespace Xrv.Core
{
    public static class Workarounds
    {
        public static void MrtkForceButtonNullPlate(Entity button)
        {
            // Workaround MTRK ignores null material on StandardButtonConfigurator
            button.FindChildrenByTag("PART_Plate", true).First().FindComponent<MaterialComponent>().Material = null;
        }

        public static void MrtkRotateButton(Entity button)
        {
            // MRTK buttons look to negative Z, so we have to invert this component
            var buttonTransform = button.FindComponent<Transform3D>();
            var rotation = buttonTransform.LocalRotation;
            rotation.Y = MathHelper.Pi;
            buttonTransform.LocalRotation = rotation;
        }
    }
}
