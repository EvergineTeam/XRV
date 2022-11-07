// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.Storage.Cache
{
    /// <summary>
    /// Considers first-level directories as cache blocks. All their contents
    /// will be part of that cache block.
    /// </summary>
    public class DefaultDirectoryBlockSelector : ICacheBlockSelector
    {
        /// <inheritdoc/>
        public string GetKey(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            var parts = relativePath
                .FixSlashes()
                .Split(DirectoryItem.SplitSeparators, StringSplitOptions.RemoveEmptyEntries);

            return parts.Length > 0 ? parts[0] : throw new InvalidOperationException($"Invalid path {relativePath}");
        }
    }
}
