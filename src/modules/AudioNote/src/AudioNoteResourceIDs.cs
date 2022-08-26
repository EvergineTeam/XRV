using System;

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

            public static readonly Guid AudioNoteAnchorBack = new Guid("55c2c50c-36c4-424c-9345-3e5c7ad8af03");
            public static readonly Guid AnchorGrabbed = new Guid("55c2c50c-36c4-424c-9355-3e5c7ad8af03");
            public static readonly Guid AnchorIdle = new Guid("55c2c50c-36c4-424b-9345-3e5c7ad8af03");
            public static readonly Guid AnchorSelected = new Guid("05c2c50c-36c4-424c-9355-3e5c7ad8af09");
            public static readonly Guid RecordingDot = new Guid("55c2c51c-36c4-425b-9345-3e5c7ad8af03");
        }

        public sealed class Prefabs
        {
            public static readonly Guid Settings = new Guid("78a5a6f4-cf3b-4611-984b-0bd411b1f050");
            public static readonly Guid Help = new Guid("b9be9e62-a68c-4eae-b52a-b12cc26b0a05");
            public static readonly Guid Anchor = new Guid("b0bb7495-b379-4af8-8458-018e0edebdb0");
            public static readonly Guid Window = new Guid("7bbcca2b-4383-4fcb-b577-bd191867a7bd");
        }
    }
}
