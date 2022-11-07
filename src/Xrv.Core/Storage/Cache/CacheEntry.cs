// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Xrv.Core.Storage.Cache
{
    /// <summary>
    /// Model for cache entries.
    /// </summary>
    public class CacheEntry
    {
        private DateTime? lastAccess;

        /// <summary>
        /// Gets cache entry size in kilobytes. This will be the sum of
        /// all files that are part of cache block.
        /// </summary>
        public long DiskSize { get => this.Paths.Sum(path => path.Value); }

        /// <summary>
        /// Gets or sets file paths that are part of cache block.
        /// </summary>
        public ConcurrentDictionary<string, long> Paths { get; set; } = new ConcurrentDictionary<string, long>();

        /// <summary>
        /// Gets or sets cache block last access datetime.
        /// </summary>
        public DateTime? LastAccess
        {
            get => this.lastAccess?.ToLocalTime();
            set => this.lastAccess = value?.ToUniversalTime();
        }
    }
}
