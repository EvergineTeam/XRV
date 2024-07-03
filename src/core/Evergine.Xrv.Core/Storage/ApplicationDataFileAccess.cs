// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Utils;
using IOFileAccess = System.IO.FileAccess;

namespace Evergine.Xrv.Core.Storage
{
    /// <summary>
    /// Provides access to application local data storage.
    /// </summary>
    public class ApplicationDataFileAccess : FileAccess
    {
        private const int FileBufferSize = 4096;
        private string basePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDataFileAccess"/> class.
        /// </summary>
        public ApplicationDataFileAccess()
        {
            this.basePath = this.ApplicationFolderPath = DeviceHelper.GetLocalApplicationFolderPath();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDataFileAccess"/> class.
        /// </summary>
        /// <param name="rootPath">Root path.</param>
        public ApplicationDataFileAccess(string rootPath)
        {
            this.basePath = this.ApplicationFolderPath = rootPath;
        }

        /// <summary>
        /// Gets application data folder path.
        /// </summary>
        public string ApplicationFolderPath { get; private set; }

        /// <inheritdoc/>
        protected override Task InternalCreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(this.basePath))
            {
                Directory.CreateDirectory(this.basePath);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task<IEnumerable<DirectoryItem>> InternalEnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var directories = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateDirectories(fullPath)
                    .Select(file => new DirectoryInfo(file))
                : Enumerable.Empty<DirectoryInfo>();

            return Task.FromResult(directories.Select(item => ConvertToDirectoryItem(item, Path.Combine(relativePath, item.Name))));
        }

        /// <inheritdoc/>
        protected override Task<IEnumerable<FileItem>> InternalEnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var files = Directory.Exists(fullPath)
                ? Directory
                    .EnumerateFiles(fullPath)
                    .Select(file => new FileInfo(file))
                : Enumerable.Empty<FileInfo>();

            return Task.FromResult(files.Select(item => ConvertToFileItem(item, Path.Combine(relativePath, item.Name))));
        }

        /// <inheritdoc/>
        protected override Task<bool> InternalExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = Directory.Exists(fullPath);
            return Task.FromResult(exists);
        }

        /// <inheritdoc/>
        protected override Task<bool> InternalExistsFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = File.Exists(fullPath);
            return Task.FromResult(exists);
        }

        /// <inheritdoc/>
        protected override async Task InternalCreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsDirectoryAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        /// <inheritdoc/>
        protected override async Task InternalWriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            await this.CreateDirectoryAsync(Path.GetDirectoryName(relativePath));

            using (var fileStream = new FileStream(fullPath, FileMode.Create, IOFileAccess.Write, FileShare.Read, FileBufferSize, true))
            {
                await stream.CopyToAsync(fileStream, FileBufferSize, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        protected override Task<Stream> InternalGetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            var stream = new FileStream(fullPath, FileMode.Open, IOFileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream>(stream);
        }

        /// <inheritdoc/>
        protected override async Task InternalDeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsDirectoryAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                Directory.Delete(fullPath, true);
            }
        }

        /// <inheritdoc/>
        protected override async Task InternalDeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsFileAsync(fullPath, cancellationToken).ConfigureAwait(false);
            if (exists)
            {
                File.Delete(fullPath);
            }
        }

        /// <inheritdoc/>
        protected override async Task<FileItem> InternalGetFileItemAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var fullPath = this.GetFullPath(relativePath);
            bool exists = await this.ExistsFileAsync(fullPath, cancellationToken).ConfigureAwait(false);
            return exists ? ConvertToFileItem(new FileInfo(fullPath), relativePath) : null;
        }

        /// <inheritdoc/>
        protected override void OnBaseDirectoryUpdate()
        {
            base.OnBaseDirectoryUpdate();
            this.basePath = string.IsNullOrEmpty(this.BaseDirectory) ? this.ApplicationFolderPath : Path.Combine(this.ApplicationFolderPath, this.BaseDirectory);
        }

        private string GetFullPath(string relativePath) => Path.Combine(this.basePath, relativePath);

        private static DirectoryItem ConvertToDirectoryItem(DirectoryInfo directory, string relativePath) =>
            new DirectoryItem(relativePath)
            {
                CreationTime = directory.CreationTime,
                ModificationTime = directory.LastWriteTime,
            };

        private static FileItem ConvertToFileItem(FileInfo file, string relativePath) =>
            new FileItem(relativePath)
            {
                CreationTime = file.CreationTime,
                ModificationTime = file.LastWriteTime,
                Size = file.Length,
            };
    }
}
