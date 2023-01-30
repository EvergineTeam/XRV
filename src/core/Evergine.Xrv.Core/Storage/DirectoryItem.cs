// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Storage
{
    /// <summary>
    /// Represents a directory.
    /// </summary>
    public sealed class DirectoryItem : FileSystemItem
    {
        /// <summary>
        /// Path separator.
        /// </summary>
        public const string PathSeparator = "/";

        /// <summary>
        /// Path separator as string array.
        /// </summary>
        public static readonly string[] SplitSeparators = new[] { PathSeparator };

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryItem"/> class.
        /// </summary>
        /// <param name="itemPath">Directory path.</param>
        public DirectoryItem(string itemPath)
            : base(itemPath)
        {
            this.Name = itemPath.GetDirectoryName();
        }
    }
}
