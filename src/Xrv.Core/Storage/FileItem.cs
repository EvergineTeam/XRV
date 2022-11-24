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
        /// <param name="itemPath">File path.</param>
        public FileItem(string itemPath)
            : base(itemPath)
        {
            this.Name = itemPath.GetFileName();
        }

        /// <summary>
        /// Gets or sets file size, in bytes.
        /// </summary>
        public long? Size { get; set; }

        /// <summary>
        /// Gets or sets MD5 hash of the file.
        /// </summary>
        public byte[] Md5Hash { get; set; }

        /// <summary>
        /// Gets a value indicating whether file has MD5 hash.
        /// </summary>
        public bool HasMd5Hash { get => this.Md5Hash?.Length > 0; }
    }
}
