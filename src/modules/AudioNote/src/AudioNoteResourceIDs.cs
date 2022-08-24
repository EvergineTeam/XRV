using System;
using System.Collections.Generic;
using System.Text;

namespace Xrv.AudioNote
{
    public sealed class AudioNoteResourceIDs
    {
        public sealed class Materials
        {
            public sealed class Icons
            {
                public static readonly Guid AudioNote = new Guid("aaeff01b-40ad-4239-b936-8c59dd77f916");
                public static readonly Guid Delete = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f919");
                public static readonly Guid Microphone = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f918");
                public static readonly Guid Play = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f915");
                public static readonly Guid Stop = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f916");
            }

            /// <summary> Asset Path (XRV/Materials/HandleGrabbed.wemt) </summary>
            public static readonly Guid HandleGrabbed = new Guid("05c2c50c-36c4-424c-9355-3e5c7ad8af09");

            /// <summary> Asset Path (XRV/Materials/HandleIdle.wemt) </summary>
            public static readonly Guid HandleIdle = new Guid("55c2c50c-36c4-424c-9355-3e5c7ad8af03");

            /// <summary> Asset Path (XRV/Materials/HandleSelected.wemt) </summary>
            public static readonly Guid HandleSelected = new Guid("5552c50c-36c4-424c-9355-3e5c7ad8af06");
        }

        public sealed class Prefabs
        {
            public static readonly Guid Settings = new Guid("78a5a6f4-cf3b-4611-984b-0bd411b1f050");
            public static readonly Guid Help = new Guid("b9be9e62-a68c-4eae-b52a-b12cc26b0a05");
        }
    }
}
