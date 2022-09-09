// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.ImageGallery
{
    /// <summary>
    /// Image Gallery resource IDs.
    /// </summary>
    public sealed class ImageGalleryResourceIDs
    {
        /// <summary>
        /// Materials.
        /// </summary>
        public sealed class Materials
        {
            /// <summary>
            /// Asset Path (Materials/ImageGalleryImage.wemt).
            /// </summary>
            public static readonly Guid ImageFrame = new Guid("e5c23ae5-06c6-48df-8d7f-b0724d158615");

            /// <summary>
            /// Icons.
            /// </summary>
            public sealed class Icons
            {
                /// <summary>
                /// Asset Path (Materials\Icons\ImageGallery.wemt).
                /// </summary>
                public static readonly Guid ImageGallery = new Guid("eaeff01b-40ad-4239-b936-8c59dd77f91a");

                /// <summary>
                /// Asset Path (Materials\Icons\Next.wemt).
                /// </summary>
                public static readonly Guid Next = new Guid("e483603c-6472-4cc9-859b-161725cc1583");

                /// <summary>
                /// Asset Path (Materials\Icons\Previous.wemt).
                /// </summary>
                public static readonly Guid Previous = new Guid("a1610892-8a76-4ba9-97fa-0e8da57f4b5a");
            }
        }

        /// <summary>
        /// Prefabs.
        /// </summary>
        public sealed class Prefabs
        {
            /// <summary>
            /// Asset Path (Prefabs/ImageGalleryWindow.wemt).
            /// </summary>
            public static readonly Guid Gallery = new Guid("2a3c11f6-fc32-4e5a-ac2a-34c3d911eb85");

            /// <summary>
            /// Asset Path (Prefabs/ImageGallerySetting.wemt).
            /// </summary>
            public static readonly Guid Settings = new Guid("23abc349-9399-49ba-a78b-4fec7fd94652");

            /// <summary>
            /// Asset Path (Prefabs/ImageGalleryHelp.wemt).
            /// </summary>
            public static readonly Guid Help = new Guid("0e6f8cc5-2149-4293-8e59-e8d02f419771");
        }
    }
}
