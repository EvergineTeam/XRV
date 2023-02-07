using Evergine.MRTK.Scenes;
using System;

namespace XrvSamples.Scenes
{
    public abstract class BaseScene : XRScene
    {
        protected override Guid CursorMatPressed => EvergineContent.MRTK.Materials.Cursor.CursorPinch;

        protected override Guid CursorMatReleased => EvergineContent.MRTK.Materials.Cursor.CursorBase;

        protected override Guid HoloHandsMat => EvergineContent.MRTK.Materials.Hands.QuestHands;

        protected override Guid SpatialMappingMat => Guid.Empty;

        protected override Guid HandRayTexture => EvergineContent.MRTK.Textures.line_dots_png;

        protected override Guid HandRaySampler => EvergineContent.MRTK.Samplers.LinearWrapSampler;

        protected override Guid LeftControllerModelPrefab => Guid.Empty;

        protected override Guid RightControllerModelPrefab => Guid.Empty;

        protected override float MaxFarCursorLength => 0.5f;
    }
}
