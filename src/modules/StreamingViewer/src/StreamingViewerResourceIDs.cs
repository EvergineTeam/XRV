// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.StreamingViewer
{
    /// <summary>
    /// Image Gallery resource IDs.
    /// </summary>
    public sealed class StreamingViewerResourceIDs
    {
        /// <summary>
        /// Materials.
        /// </summary>
        public sealed class Materials
        {
            /// <summary>
            /// Asset Path (Materials/VideoFrameMaterial.wemt).
            /// </summary>
            public static readonly Guid VideoFrameMaterial = new Guid("724be777-bffd-4564-a75f-55f4bd394ae3");

            /// <summary>
            /// Icons.
            /// </summary>
            public sealed class Icons
            {
                /// <summary>
                /// Asset Path (Materials/Icons/StreamingViewer.wemt).
                /// </summary>
                public static readonly Guid StreamingViewer = new Guid("e6eff01b-40ad-4239-b936-8c59dd77f91a");
            }
        }

        /// <summary>
        /// Prefabs.
        /// </summary>
        public sealed class Prefabs
        {
            /// <summary> Asset Path (XRV/Prefabs/StreamingViewer.weprefab.weprf). </summary>
            public static readonly Guid StreamingViewer_weprefab = new Guid("cad55a3e-637a-4509-b059-b844ef5c2822");

            /// <summary> Asset Path (XRV/Prefabs/StreamingViewerHelp.weprefab.weprf). </summary>
            public static readonly Guid StreamingViewerHelp_weprefab = new Guid("e52f23d4-b606-40b1-963f-e851afcfcf20");

            /// <summary> Asset Path (XRV/Prefabs/StreamListWindow.weprefab.weprf). </summary>
            public static readonly Guid StreamingListWindow_weprefab = new Guid("9590fd11-aca6-47b6-9027-5610fa9b0403");
        }
    }
}
