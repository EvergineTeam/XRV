// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Storage
{
    /// <summary>
    /// Represents a file system item (file or directory).
    /// </summary>
    public abstract class FileSystemItem
    {
        private DateTime? creationTime;
        private DateTime? modificationTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemItem"/> class.
        /// </summary>
        /// <param name="itemPath">File or directory path.</param>
        protected FileSystemItem(string itemPath)
        {
            // we want same path separator in any implementation
            // of FileAccess, this is why we use FixSlashes extension method
            this.Path = itemPath.FixSlashes();
        }

        /// <summary>
        /// Gets or sets item name.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets item path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets or sets item creation time. It is saved in UTC and
        /// returned as local datetime.
        /// </summary>
        public DateTime? CreationTime
        {
            get => this.creationTime?.ToLocalTime();

            set => this.creationTime = value?.ToUniversalTime();
        }

        /// <summary>
        /// Gets or sets item modification time. It is saved in UTC and
        /// returned as local datetime.
        /// </summary>
        public DateTime? ModificationTime
        {
            get => this.modificationTime?.ToLocalTime();

            set => this.modificationTime = value?.ToUniversalTime();
        }
    }
}
