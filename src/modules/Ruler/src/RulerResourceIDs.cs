using System;
using System.Collections.Generic;
using System.Text;

namespace Xrv.Ruler
{
    public sealed class RulerResourceIDs
    {
        public sealed class Materials
        {
            public sealed class Icons
            {
                /// <summary> Asset Path (Materials/Icons/Measure.wemt) </summary>
                public static readonly Guid Measure = new Guid("da0dcd75-78ac-4abf-a2af-011c5ebd458e");
            }

            /// <summary> Asset Path (XRV/Materials/HandleGrabbed.wemt) </summary>
            public static readonly Guid HandleGrabbed = new Guid("05c2c50c-36c4-424c-9355-3e5c7ad8af04");

            /// <summary> Asset Path (XRV/Materials/HandleIdle.wemt) </summary>
            public static readonly Guid HandleIdle = new Guid("55c2c50c-36c4-424c-9355-3e5c7ad8af04");

            /// <summary> Asset Path (XRV/Materials/HandleSelected.wemt) </summary>
            public static readonly Guid HandleSelected = new Guid("5552c50c-36c4-424c-9355-3e5c7ad8af04");
        }

        public sealed class Prefabs
        {
            /// <summary> Asset Path (Prefabs/Ruler.weprefab.weprf) </summary>
            public static readonly Guid Ruler_weprefab = new Guid("b28d413a-da9d-4269-b289-6c98f05e1b6f");

            /// <summary> Asset Path (XRV/Prefabs/RulerHelp.weprefab.weprf) </summary>
            public static readonly Guid RulerHelp_weprefab = new Guid("ee76d37e-f934-4583-b50a-09f61a1e4e79");
        }
    }
}
