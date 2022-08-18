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
        }

        public sealed class Prefabs
        {
            /// <summary> Asset Path (Prefabs/Ruler.weprefab.weprf) </summary>
            public static readonly Guid Ruler_weprefab = new Guid("b3008ac5-fdc9-49f0-a651-8cc6a8aa9a1f");
        }
    }
}
