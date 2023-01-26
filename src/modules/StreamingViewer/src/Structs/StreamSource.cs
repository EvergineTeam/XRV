// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Xrv.Core.Storage;

namespace Xrv.StreamingViewer.Structs
{
    /// <summary>
    /// Repository information.
    /// </summary>
    public struct StreamSource
    {
        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the fileAccess.
        /// </summary>
        public string Url { get; set; }
    }
}
