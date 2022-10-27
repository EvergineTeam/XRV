// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Storage
{
    /// <summary>
    /// Represents a directory.
    /// </summary>
    public sealed class DirectoryItem : FileSystemItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryItem"/> class.
        /// </summary>
        /// <param name="name">Directory name.</param>
        public DirectoryItem(string name)
            : base(name)
        {
        }
    }
}
