// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Storage
{
    /// <summary>
    /// Represents a file.
    /// </summary>
    public sealed class FileItem : FileSystemItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileItem"/> class.
        /// </summary>
        /// <param name="name">File name.</param>
        public FileItem(string name)
            : base(name)
        {
        }
    }
}
