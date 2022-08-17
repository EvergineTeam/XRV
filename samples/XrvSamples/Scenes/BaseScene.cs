using Evergine.MRTK.Scenes;
using System;
using Xrv.Core;

namespace XrvSamples.Scenes
{
    public abstract class BaseScene : XRScene
    {
        protected override Guid CursorMatPressed => DefaultResourceIDs.Mrtk.Materials.Cursor.CursorPinch;

        protected override Guid CursorMatReleased => DefaultResourceIDs.Mrtk.Materials.Cursor.CursorBase;

        protected override Guid HoloHandsMat => Guid.Empty;

        protected override Guid SpatialMappingMat => Guid.Empty;

        protected override Guid HandRayTexture => DefaultResourceIDs.Mrtk.Textures.line_dots_png;

        protected override Guid HandRaySampler => DefaultResourceIDs.Mrtk.Samplers.LinearWrapSampler;
    }
}
