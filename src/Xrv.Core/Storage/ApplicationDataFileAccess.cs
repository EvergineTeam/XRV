// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IOFileAccess = System.IO.FileAccess;

namespace Xrv.Core.Storage
{
    /// <summary>
    /// Provides access to application local data storage.
    /// </summary>
    public class ApplicationDataFileAccess : FileAccess
    {
        private const int FileBufferSize = 4096;
        private readonly string rootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDataFileAccess"/> class.
        /// </summary>
        public ApplicationDataFileAccess()
        {
            if (DeviceInfo.PlatformType == Evergine.Common.PlatformType.Windows
                &&
                Assembly.GetEntryAssembly() is Assembly entryAssembly)
            {
                this.rootPath = Path.GetDirectoryName(entryAssembly.Location);
            }
            else
            {
                this.rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<string>> EnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var directories = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateDirectories(fullPath)
                    .Select(file => Path.GetFileName(file))
                : Enumerable.Empty<string>();

            return Task.FromResult(directories);
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<string>> EnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var files = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateFiles(fullPath)
                    .Select(file => Path.GetFileName(file))
                : Enumerable.Empty<string>();

            return Task.FromResult(files);
        }

        /// <inheritdoc/>
        public override Task<bool> ExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = Directory.Exists(fullPath);
            return Task.FromResult(exists);
        }

        /// <inheritdoc/>
        public override Task<bool> ExistsFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = File.Exists(fullPath);
            return Task.FromResult(exists);
        }

        /// <inheritdoc/>
        public override async Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsDirectoryAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        /// <inheritdoc/>
        public override async Task WriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            using (var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, IOFileAccess.Write, FileShare.Write, FileBufferSize, true))
            {
                await stream.CopyToAsync(fileStream, FileBufferSize, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override Task<Stream> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var stream = new FileStream(fullPath, FileMode.Open, IOFileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream>(stream);
        }

        /// <inheritdoc/>
        public override async Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsDirectoryAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                Directory.Delete(fullPath, true);
            }
        }

        /// <inheritdoc/>
        public override async Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsFileAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                File.Delete(fullPath);
            }
        }

        private string GetFullPath(string relativePath) => Path.Combine(this.rootPath, relativePath);
    }
}
