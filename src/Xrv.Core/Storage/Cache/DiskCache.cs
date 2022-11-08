// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Xrv.Core.Storage.Cache
{
    /// <summary>
    /// Uses disk as cache for files.
    /// </summary>
    public class DiskCache : ApplicationDataFileAccess
    {
        internal const string CacheRootFolderName = "cache";
        internal const string CacheStatusFileName = "__cache";

        private readonly ConcurrentDictionary<string, CacheEntry> cacheEntries;
        private readonly ReadOnlyDictionary<string, CacheEntry> readOnlyCacheEntries;
        private readonly ICacheBlockSelector blockSelector;
        private readonly SemaphoreSlim statusFileSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskCache"/> class.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        public DiskCache(string cacheName)
            : this(cacheName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskCache"/> class.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        /// <param name="keySelector">Cache key selection strategy.</param>
        public DiskCache(string cacheName, ICacheBlockSelector keySelector)
        {
            this.cacheEntries = new ConcurrentDictionary<string, CacheEntry>();
            this.readOnlyCacheEntries = new ReadOnlyDictionary<string, CacheEntry>(this.cacheEntries);
            this.blockSelector = keySelector ?? new DefaultFileBlockSelector();
            this.statusFileSemaphore = new SemaphoreSlim(1);
            base.BaseDirectory = $"{CacheRootFolderName}/{cacheName}";
        }

        /// <inheritdoc />
        public override string BaseDirectory
        {
            get => base.BaseDirectory;
            set => throw new InvalidOperationException($"Can't set base directory for {nameof(DiskCache)}");
        }

        /// <inheritdoc />
        public override DiskCache Cache
        {
            get => base.Cache;
            set
            {
                if (value != null)
                {
                    throw new InvalidOperationException("Cache of a cache? Don't think so...");
                }
            }
        }

        /// <summary>
        /// Gets cache entries.
        /// </summary>
        public IReadOnlyDictionary<string, CacheEntry> CacheEntries { get => this.readOnlyCacheEntries; }

        /// <summary>
        /// Gets or sets cache size limits on kilobytes. If disk usage of cache
        /// exceeds this limit, some cache entries will be removed until size is under
        /// the limit again.
        /// </summary>
        public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets indicates maximum time that cache entries may remain in cache if not accessed.
        /// Once a file is accessed, it resets this counter to initial state.
        /// </summary>
        public TimeSpan SlidingExpiration { get; set; } = TimeSpan.MaxValue;

        /// <summary>
        /// Gets current cache size, in kilobytes.
        /// </summary>
        public long CurrentCacheSize { get => this.cacheEntries.Sum(entry => entry.Value.DiskSize); }

        /// <summary>
        /// Initializes cache, creating cache folder if not existing. All cache elements that
        /// do not comply <see cref="SlidingExpiration"/> and/or <see cref="SizeLimit"/> constraints
        /// will be removed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await this.CreateBaseDirectoryIfNotExistsAsync(cancellationToken).ConfigureAwait(false);
            await this.UpdateCacheEntriesAsync(cancellationToken).ConfigureAwait(false);
            await this.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Flushes cache contents, removing all files that do not pass current constraints.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public Task FlushAsync(CancellationToken cancellationToken = default) =>
            this.FlushAsync(null, cancellationToken);

        /// <inheritdoc/>
        protected override async Task<IEnumerable<FileItem>> InternalEnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var files = await base.InternalEnumerateFilesAsync(relativePath, cancellationToken).ConfigureAwait(false);
            bool isRootFolder = string.IsNullOrEmpty(relativePath);
            if (isRootFolder)
            {
                files = files.Where(file => file.Name != CacheStatusFileName);
            }

            return files;
        }

        /// <inheritdoc/>
        protected override async Task<Stream> InternalGetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var stream = await base.InternalGetFileAsync(relativePath, cancellationToken).ConfigureAwait(false);
            if (relativePath == CacheStatusFileName)
            {
                return stream;
            }

            var cacheKey = this.blockSelector.GetKey(relativePath);
            if (this.cacheEntries.TryGetValue(cacheKey, out var cacheEntry))
            {
                cacheEntry.LastAccess = DateTime.UtcNow;
            }

            await this.SaveStatusAsync(cancellationToken).ConfigureAwait(false);

            return stream;
        }

        /// <inheritdoc/>
        protected override async Task InternalWriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            // Save file and add register into cache
            await base.InternalWriteFileAsync(relativePath, stream, cancellationToken).ConfigureAwait(false);
            if (relativePath == CacheStatusFileName)
            {
                return;
            }

            var cacheKey = this.blockSelector.GetKey(relativePath);
            var cacheEntry = new CacheEntry()
            {
                LastAccess = DateTime.UtcNow,
            };

            long streamLength = stream.CanSeek ? stream.Length : stream.Position;
            cacheEntry.Paths[relativePath] = streamLength;

            this.cacheEntries.AddOrUpdate(cacheKey, cacheEntry, (key, existing) =>
            {
                existing.LastAccess = cacheEntry.LastAccess;
                existing.Paths.AddOrUpdate(relativePath, streamLength, (key, oldValue) => streamLength);

                return existing;
            });

            await this.FlushAsync(relativePath, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task InternalDeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            await base.InternalDeleteFileAsync(relativePath, cancellationToken).ConfigureAwait(false);

            var cacheKey = this.blockSelector.GetKey(relativePath);
            if (this.cacheEntries.TryGetValue(cacheKey, out var cacheEntry))
            {
                cacheEntry.Paths.TryRemove(relativePath, out var _);
                if (!cacheEntry.Paths.Any())
                {
                    this.cacheEntries.TryRemove(cacheKey, out var _);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task InternalClearAsync(CancellationToken cancellationToken = default)
        {
            await base.InternalClearAsync(cancellationToken).ConfigureAwait(false);
            await this.DeleteFileAsync(CacheStatusFileName, cancellationToken).ConfigureAwait(false);
        }

        private async Task InternalFlushBySlidingAsync(CancellationToken cancellationToken)
        {
            if (this.SlidingExpiration == TimeSpan.MaxValue)
            {
                return;
            }

            var lowestValidDateTime = DateTime.UtcNow.Add(-this.SlidingExpiration).ToLocalTime();
            var expiredEntries = this.cacheEntries.Where(entry => entry.Value.LastAccess < lowestValidDateTime);
            foreach (var entry in expiredEntries)
            {
                await this.DeleteFileAsync(entry.Key, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task FlushAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var cacheKey = relativePath != null ? this.blockSelector.GetKey(relativePath) : string.Empty;

            // Check sliding expiration
            await this.InternalFlushBySlidingAsync(cancellationToken).ConfigureAwait(false);

            // Check cache size limits
            var cacheSizeDiff = this.CurrentCacheSize - this.SizeLimit;
            if (cacheSizeDiff > 0)
            {
                await this.InternalFlushBySizeAzync(cacheSizeDiff, cacheKey, cancellationToken).ConfigureAwait(false);
            }

            await this.SaveStatusAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task InternalFlushBySizeAzync(long sizeToRelease, string cacheKey, CancellationToken cancellationToken)
        {
            long releasedSize = 0;

            if (!this.cacheEntries.Any())
            {
                return;
            }

            do
            {
                var oldestEntry = this.cacheEntries
                    .Where(entry => entry.Key != cacheKey) // exclude current key (last added)
                    .OrderBy(entry => entry.Value.LastAccess)
                    .FirstOrDefault();
                if (oldestEntry.Key == default)
                {
                    // no more entries to remove
                    break;
                }

                foreach (var cacheItem in oldestEntry.Value.Paths)
                {
                    string filePath = cacheItem.Key;
                    await this.DeleteFileAsync(filePath, cancellationToken).ConfigureAwait(false);
                    releasedSize += cacheItem.Value;

                    var directoryName = Path.GetDirectoryName(filePath);
                    var directoryFiles = await this.EnumerateFilesAsync(directoryName, cancellationToken).ConfigureAwait(false);
                    if (!directoryFiles.Any())
                    {
                        await this.DeleteDirectoryAsync(directoryName, cancellationToken).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            while (releasedSize < sizeToRelease && this.cacheEntries.Any());
        }

        private async Task UpdateCacheEntriesAsync(CancellationToken cancellationToken)
        {
            await this.LoadStatusAsync(cancellationToken).ConfigureAwait(false);
            await this.UpdateCacheEntriesFromDirectoryAsync(string.Empty, cancellationToken).ConfigureAwait(false);
        }

        private async Task UpdateCacheEntriesFromDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            bool isRootPath = string.IsNullOrEmpty(directoryPath);
            var files = await this.EnumerateFilesAsync(directoryPath, cancellationToken).ConfigureAwait(false);
            foreach (var file in files)
            {
                if (isRootPath && file.Name == CacheStatusFileName)
                {
                    continue;
                }

                var filePath = file.Path;
                var fileSize = file.Size ?? 0;
                var cacheKey = this.blockSelector.GetKey(file.Path);
                var cacheEntry = new CacheEntry();
                cacheEntry.Paths.TryAdd(filePath, fileSize);

                this.cacheEntries.AddOrUpdate(cacheKey, cacheEntry, (key, existing) =>
                {
                    if (existing.LastAccess < cacheEntry.LastAccess)
                    {
                        existing.LastAccess = cacheEntry.LastAccess;
                    }

                    existing.Paths.AddOrUpdate(filePath, fileSize, (key, existing) => fileSize);
                    return existing;
                });
            }

            var directories = await this.EnumerateDirectoriesAsync(directoryPath, cancellationToken).ConfigureAwait(false);
            foreach (var directory in directories)
            {
                await this.UpdateCacheEntriesFromDirectoryAsync(directory.Path, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task LoadStatusAsync(CancellationToken cancellationToken = default)
        {
            await this.statusFileSemaphore.WaitAsync();

            try
            {
                this.cacheEntries.Clear();

                Dictionary<string, long> pathsFiltering = new Dictionary<string, long>();

                if (await this.ExistsFileAsync(CacheStatusFileName, cancellationToken).ConfigureAwait(false))
                {
                    using (var stream = await this.GetFileAsync(CacheStatusFileName, cancellationToken).ConfigureAwait(false))
                    {
                        var savedStatus = await JsonSerializer
                            .DeserializeAsync<Dictionary<string, CacheEntry>>(stream, cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        foreach (var entry in savedStatus)
                        {
                            pathsFiltering.Clear();

                            foreach (var path in entry.Value.Paths)
                            {
                                bool fileExists = await this.ExistsFileAsync(path.Key, cancellationToken).ConfigureAwait(false);
                                if (fileExists)
                                {
                                    pathsFiltering.Add(path.Key, path.Value);
                                }
                            }

                            entry.Value.Paths.Clear();
                            foreach (var path in pathsFiltering)
                            {
                                entry.Value.Paths.TryAdd(path.Key, path.Value);
                            }

                            if (entry.Value.Paths.Any())
                            {
                                this.cacheEntries.TryAdd(entry.Key, entry.Value);
                            }
                        }
                    }
                }
            }
            finally
            {
                this.statusFileSemaphore.Release();
            }
        }

        private async Task SaveStatusAsync(CancellationToken cancellationToken = default)
        {
            await this.statusFileSemaphore.WaitAsync();

            try
            {
                using (var stream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(stream, this.cacheEntries, cancellationToken: cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    stream.Seek(0, SeekOrigin.Begin);
                    await this.WriteFileAsync(CacheStatusFileName, stream, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                this.statusFileSemaphore.Release();
            }
        }
    }
}
