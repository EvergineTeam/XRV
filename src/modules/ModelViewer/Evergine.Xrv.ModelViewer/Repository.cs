// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.Storage;

namespace Evergine.Xrv.ModelViewer
{
    /// <summary>
    /// Repository information.
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the fileAccess.
        /// </summary>
        public FileAccess FileAccess { get; set; }
    }
}
