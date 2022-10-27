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
        private string rootPath;
        private string basePath;

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

            this.basePath = this.rootPath;
        }

        /// <inheritdoc/>
        public override Task CreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(this.basePath))
            {
                Directory.CreateDirectory(this.basePath);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<DirectoryItem>> EnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var directories = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateDirectories(fullPath)
                    .Select(file => new DirectoryInfo(file))
                : Enumerable.Empty<DirectoryInfo>();

            return Task.FromResult(directories.Select(ConvertToDirectoryItem));
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<FileItem>> EnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var files = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateFiles(fullPath)
                    .Select(file => new FileInfo(file))
                : Enumerable.Empty<FileInfo>();

            return Task.FromResult(files.Select(ConvertToFileItem));
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

        /// <inheritdoc/>
        protected override void OnBaseDirectoryUpdate()
        {
            base.OnBaseDirectoryUpdate();
            this.basePath = string.IsNullOrEmpty(this.BaseDirectory) ? this.rootPath : Path.Combine(this.rootPath, this.BaseDirectory);
        }

        private string GetFullPath(string relativePath) => Path.Combine(this.basePath, relativePath);

        private static DirectoryItem ConvertToDirectoryItem(DirectoryInfo directory) =>
            new DirectoryItem(directory.Name)
            {
                CreationTime = directory.CreationTime,
                ModificationTime = directory.LastWriteTime,
            };

        private static FileItem ConvertToFileItem(FileInfo file) =>
            new FileItem(file.Name)
            {
                CreationTime = file.CreationTime,
                ModificationTime = file.LastWriteTime,
            };
    }
}
