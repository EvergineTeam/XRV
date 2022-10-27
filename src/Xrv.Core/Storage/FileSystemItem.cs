// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.Storage
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
        /// <param name="name">File or directory name.</param>
        protected FileSystemItem(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets item name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets item creation time.
        /// </summary>
        public DateTime? CreationTime
        {
            get => this.creationTime?.ToLocalTime();

            set => this.creationTime = value?.ToUniversalTime();
        }

        /// <summary>
        /// Gets or sets item modification time.
        /// </summary>
        public DateTime? ModificationTime
        {
            get => this.modificationTime?.ToLocalTime();

            set => this.modificationTime = value?.ToUniversalTime();
        }
    }
}
