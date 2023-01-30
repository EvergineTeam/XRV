// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Storage.Cache;

namespace Evergine.Xrv.Core.Storage
{
    /// <summary>
    /// Provides access to files and directories.
    /// </summary>
    public abstract class FileAccess
    {
        private string baseDirectory;
        private DiskCache cache;

        /// <summary>
        /// Gets or sets base directory.
        /// </summary>
        public virtual string BaseDirectory
        {
            get => this.baseDirectory;

            set
            {
                if (this.baseDirectory != value)
                {
                    this.baseDirectory = value;
                    this.OnBaseDirectoryUpdate();
                }
            }
        }

        /// <summary>
        /// Gets or sets disk cache.
        /// </summary>
        public virtual DiskCache Cache
        {
            get => this.cache;
            set => this.cache = value;
        }

        /// <summary>
        /// Gets a value indicating whether cache is enabled.
        /// </summary>
        public bool IsCachingEnabled { get => this.cache != null; }

        /// <summary>
        /// Ensures that <see cref="BaseDirectory" /> exist, creating it if not.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task CreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default) =>
            this.InternalCreateBaseDirectoryIfNotExistsAsync(cancellationToken);

        /// <summary>
        /// Enumerates directory from root directory.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of directories under root directory, first level only.</returns>
        public virtual Task<IEnumerable<DirectoryItem>> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default) =>
            this.EnumerateDirectoriesAsync(string.Empty, cancellationToken);

        /// <summary>
        /// Enumerates directory from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children directories, first level only.</returns>
        public virtual async Task<IEnumerable<DirectoryItem>> EnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            bool succeeded = false;
            IEnumerable<DirectoryItem> directories = null;

            try
            {
                directories = await this.InternalEnumerateDirectoriesAsync(relativePath, cancellationToken).ConfigureAwait(false);
                succeeded = true;
            }
            catch (Exception ex) when (this.EvaluateCacheUsageOnException(ex))
            {
                Trace.TraceWarning($"Could not retrieve directories, retrieving cached data instead: {ex}");
            }

            if (!succeeded && this.IsCachingEnabled)
            {
                directories = await this.cache.EnumerateDirectoriesAsync(relativePath, cancellationToken).ConfigureAwait(false);
            }

            return directories;
        }

        /// <summary>
        /// Enumerates files from root directory.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of files under root directory, first level only.</returns>
        public virtual Task<IEnumerable<FileItem>> EnumerateFilesAsync(CancellationToken cancellationToken = default) =>
            this.EnumerateFilesAsync(string.Empty, cancellationToken);

        /// <summary>
        /// Enumerates files from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children files, first level only.</returns>
        public virtual async Task<IEnumerable<FileItem>> EnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            bool succeeded = false;
            IEnumerable<FileItem> files = null;

            try
            {
                files = await this.InternalEnumerateFilesAsync(relativePath, cancellationToken).ConfigureAwait(false);
                succeeded = true;
            }
            catch (Exception ex) when (this.EvaluateCacheUsageOnException(ex))
            {
                Trace.TraceWarning($"Could not retrieve files, retrieving cached data instead: {ex}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!succeeded && this.IsCachingEnabled)
            {
                files = await this.cache.EnumerateFilesAsync(relativePath, cancellationToken).ConfigureAwait(false);
            }

            return files;
        }

        /// <summary>
        /// Checks that a directory exists.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if directory exits; false otherwise.</returns>
        public virtual Task<bool> ExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalExistsDirectoryAsync(relativePath, cancellationToken);

        /// <summary>
        /// Checks that a file exists.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if file exits; false otherwise.</returns>
        public virtual Task<bool> ExistsFileAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalExistsFileAsync(relativePath, cancellationToken);

        /// <summary>
        /// Create a directory if it does not exist. If a folder hierarchy is provided, any unexisting directory
        /// will be created.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalCreateDirectoryAsync(relativePath, cancellationToken);

        /// <summary>
        /// Creates or overrides a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="stream">File contents stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task WriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default) =>
            this.InternalWriteFileAsync(relativePath, stream, cancellationToken);

        /// <summary>
        /// Retrieves a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File contents stream.</returns>
        public virtual async Task<Stream> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fileItem = await this.GetFileItemAsync(relativePath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            Stream stream = this.IsCachingEnabled
                ? await this.RetrieveItemFromCacheAsync(relativePath, fileItem, cancellationToken).ConfigureAwait(false)
                : null;

            if (cancellationToken.IsCancellationRequested)
            {
                stream?.Dispose();
                return null;
            }

            if (stream == null)
            {
                stream = await this.InternalGetFileAsync(relativePath, cancellationToken).ConfigureAwait(false);
                if (cancellationToken.IsCancellationRequested)
                {
                    stream?.Dispose();
                    return null;
                }

                if (this.IsCachingEnabled)
                {
                    stream = await this.SaveItemToCacheAsync(fileItem, stream, cancellationToken).ConfigureAwait(false);
                }
            }

            return stream;
        }

        /// <summary>
        /// Deletes a directory, if exits.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalDeleteDirectoryAsync(relativePath, cancellationToken);

        /// <summary>
        /// Deletes a file, if exits.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalDeleteFileAsync(relativePath, cancellationToken);

        /// <summary>
        /// Gets file information.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File information.</returns>
        public virtual Task<FileItem> GetFileItemAsync(string relativePath, CancellationToken cancellationToken = default) =>
            this.InternalGetFileItemAsync(relativePath, cancellationToken);

        /// <summary>
        /// Clears file access contents, removing any present folder or file.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public virtual Task ClearAsync(CancellationToken cancellationToken = default) => this.InternalClearAsync(cancellationToken);

        /// <summary>
        /// Ensures that <see cref="BaseDirectory" /> exist, creating it if not.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected abstract Task InternalCreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates directory from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children directories, first level only.</returns>
        protected abstract Task<IEnumerable<DirectoryItem>> InternalEnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates files from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children files, first level only.</returns>
        protected abstract Task<IEnumerable<FileItem>> InternalEnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that a directory exists.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if directory exits; false otherwise.</returns>
        protected abstract Task<bool> InternalExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that a file exists.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if file exits; false otherwise.</returns>
        protected abstract Task<bool> InternalExistsFileAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a directory if it does not exist. If a folder hierarchy is provided, any unexisting directory
        /// will be created.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected abstract Task InternalCreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or overrides a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="stream">File contents stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected abstract Task InternalWriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File contents stream.</returns>
        protected abstract Task<Stream> InternalGetFileAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a directory, if exits.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected abstract Task InternalDeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file, if exits.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected abstract Task InternalDeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File information.</returns>
        protected abstract Task<FileItem> InternalGetFileItemAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears file access contents, removing any present folder or file.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task InternalClearAsync(CancellationToken cancellationToken = default)
        {
            var directories = await this.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var directory in directories)
            {
                await this.DeleteDirectoryAsync(directory.Name).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            var files = await this.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var file in files)
            {
                await this.DeleteFileAsync(file.Name).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        /// <summary>
        /// Evaluates if a catched exception retrieving data should be ignored and
        /// use cache instead.
        /// </summary>
        /// <param name="exception">Catched exception.</param>
        /// <returns>True if cache should be used; false otherwise.</returns>
        protected virtual bool InternalEvaluateCacheUsageOnException(Exception exception) => false;

        /// <summary>
        /// Invoked when base directory path is updated.
        /// </summary>
        protected virtual void OnBaseDirectoryUpdate()
        {
        }

        private bool EvaluateCacheUsageOnException(Exception exception) =>
            this.IsCachingEnabled ? this.InternalEvaluateCacheUsageOnException(exception) : false;

        private async Task<Stream> RetrieveItemFromCacheAsync(string relativePath, FileItem fileItem, CancellationToken cancellationToken)
        {
            Stream stream = null;

            fileItem = fileItem ?? await this.GetFileItemAsync(relativePath, cancellationToken).ConfigureAwait(false);

            // Evaluate if file is already cached
            bool exists = await this.cache.ExistsFileAsync(relativePath, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (exists)
            {
                // Compare cache modification date with source modification date. If current cached
                // date is lower than source date, we should not retrieve item from cache.
                var cacheFileItem = await this.cache.GetFileItemAsync(relativePath, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                if (cacheFileItem.ModificationTime < fileItem.ModificationTime)
                {
                    Trace.TraceInformation($"Current version of {relativePath} is not valid.");
                }
                else
                {
                    Trace.TraceInformation($"Found valid cached data for {relativePath}.");
                    stream = await this.cache.GetFileAsync(relativePath, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                Trace.TraceInformation($"Could not find a cached version of {relativePath}.");
            }

            return stream;
        }

        private async Task<Stream> SaveItemToCacheAsync(FileItem fileItem, Stream stream, CancellationToken cancellationToken = default)
        {
            Trace.TraceInformation($"Caching version of {fileItem.Path}.");

            // First save file into cache
            await this.cache.WriteFileAsync(fileItem.Path, stream, cancellationToken).ConfigureAwait(false);

            // Then check its integrity: file size and file checksum (when available)
            stream = await this.cache.GetFileAsync(fileItem.Path, cancellationToken).ConfigureAwait(false);
            bool integrityOk = fileItem.Size.HasValue
                ? fileItem.Size == (stream.CanSeek ? stream.Length : stream.Position)
                : true;
            if (integrityOk && fileItem.HasMd5Hash)
            {
                integrityOk = this.ValidateMd5Hash(stream, fileItem.Md5Hash);
            }

            if (!integrityOk)
            {
                Trace.TraceWarning($"File {fileItem.Path} integrity check not succeeded, removing file...");
                await this.cache.DeleteFileAsync(fileItem.Path, cancellationToken).ConfigureAwait(false);
                throw new FileIntegrityException($"File {fileItem.Path} integrity check failed");
            }

            // Ensure stream is going to be returned from the beginning
            if (stream.CanSeek == true)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                stream = await this.cache.GetFileAsync(fileItem.Path, cancellationToken).ConfigureAwait(false);
            }

            return stream;
        }

        private bool ValidateMd5Hash(Stream fileStream, byte[] expectedHash)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(fileStream);
                bool isValid = hash.SequenceEqual(expectedHash);
                return isValid;
            }
        }
    }
}
