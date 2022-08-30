// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.AudioNote
{
    /// <summary>
    /// Audio note resource IDs.
    /// </summary>
    public sealed class AudioNoteResourceIDs
    {
        /// <summary>
        /// Materials.
        /// </summary>
        public sealed class Materials
        {
            /// <summary>
            /// Asset Path (Materials/AudioNoteAnchorBack.wemt).
            /// </summary>
            public static readonly Guid AudioNoteAnchorBack = new Guid("55c2c50c-36c4-424c-9345-3e5c7ad8af03");

            /// <summary>
            /// Asset Path (Materials/AnchorGrabbed.wemt).
            /// </summary>
            public static readonly Guid AnchorGrabbed = new Guid("55c2c50c-36c4-424c-9355-3e5c7ad8af03");

            /// <summary>
            /// Asset Path (Materials/AnchorIdle.wemt).
            /// </summary>
            public static readonly Guid AnchorIdle = new Guid("55c2c50c-36c4-424b-9345-3e5c7ad8af03");

            /// <summary>
            /// Asset Path (Materials/AnchorSelected.wemt).
            /// </summary>
            public static readonly Guid AnchorSelected = new Guid("05c2c50c-36c4-424c-9355-3e5c7ad8af09");

            /// <summary>
            /// Asset Path (Materials/RecordingDot.wemt).
            /// </summary>
            public static readonly Guid RecordingDot = new Guid("55c2c51c-36c4-425b-9345-3e5c7ad8af03");

            /// <summary>
            /// Icons.
            /// </summary>
            public sealed class Icons
            {
                /// <summary>
                /// Asset Path (Materials/Icons/AudioNote.wemt).
                /// </summary>
                public static readonly Guid AudioNote = new Guid("aaeff01b-40ad-4239-b936-8c59dd77f916");

                /// <summary>
                /// Asset Path (Materials/Icons/Delete.wemt).
                /// </summary>
                public static readonly Guid Delete = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f919");

                /// <summary>
                /// Asset Path (Materials/Icons/Microphone.wemt).
                /// </summary>
                public static readonly Guid Microphone = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f918");

                /// <summary>
                /// Asset Path (Materials/Icons/Play.wemt).
                /// </summary>
                public static readonly Guid Play = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f915");

                /// <summary>
                /// Asset Path (Materials/Icons/Stop.wemt).
                /// </summary>
                public static readonly Guid Stop = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f916");
            }
        }

        /// <summary>
        /// Prefabs.
        /// </summary>
        public sealed class Prefabs
        {
            /// <summary>
            /// Asset Path (Prefabs/Settings.wemt).
            /// </summary>
            public static readonly Guid Settings = new Guid("78a5a6f4-cf3b-4611-984b-0bd411b1f050");

            /// <summary>
            /// Asset Path (Prefabs/Help.wemt).
            /// </summary>
            public static readonly Guid Help = new Guid("b9be9e62-a68c-4eae-b52a-b12cc26b0a05");

            /// <summary>
            /// Asset Path (Prefabs/Anchor.wemt).
            /// </summary>
            public static readonly Guid Anchor = new Guid("b0bb7495-b379-4af8-8458-018e0edebdb0");

            /// <summary>
            /// Asset Path (Prefabs/Window.wemt).
            /// </summary>
            public static readonly Guid Window = new Guid("7bbcca2b-4383-4fcb-b577-bd191867a7bd");
        }

        /// <summary>
        /// Audio.
        /// </summary>
        public sealed class Audio
        {
            /// <summary>
            /// Asset Path (Audio/Sample.wemt).
            /// </summary>
            public static readonly Guid Sample = new Guid("0537f12d-fd80-4638-84cf-b2aa256e979b");
        }
    }
}
