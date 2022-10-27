// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Azure;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xrv.Core.Storage
{
    /// <summary>
    /// File access for Azure Blobs.
    /// </summary>
    public class AzureBlobFileAccess : FileAccess
    {
        /*
         * Note: Please, remember that in Azure Storage Blobs, there is no concept for directories.
         */

        private const string PathDelimiter = @"/";
        private readonly BlobContainerClient container;
        private readonly string[] prefixSplit = new string[] { PathDelimiter };

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileAccess"/> class.
        /// </summary>
        /// <param name="container">Blob container client.</param>
        public AzureBlobFileAccess(BlobContainerClient container)
        {
            this.container = container;
        }

        /// <inheritdoc/>
        public override Task CreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <summary>
        /// Creates a blob access instance from connection string and container name.
        /// </summary>
        /// <param name="connectionString">Azure storage account connection string.</param>
        /// <param name="containerName">Container name.</param>
        /// <returns>File access instance.</returns>
        public static AzureBlobFileAccess CreateFromConnectionString(string connectionString, string containerName)
        {
            var container = new BlobContainerClient(connectionString, containerName);
            return new AzureBlobFileAccess(container);
        }

        /// <summary>
        /// Creates a blob access instance from container URI.
        /// </summary>
        /// <param name="containerUri">Container URI.</param>
        /// <returns>File access instance.</returns>
        public static AzureBlobFileAccess CreateFromUri(Uri containerUri)
        {
            var builder = new UriBuilder(containerUri);
            var credentials = new AzureSasCredential(builder.Query);
            builder.Query = null;

            var container = new BlobContainerClient(builder.Uri, credentials);
            return new AzureBlobFileAccess(container);
        }

        /// <summary>
        /// Creates a blob access instance from container URI and SAS token.
        /// </summary>
        /// <param name="containerUri">Container URI.</param>
        /// <param name="signature">SAS token.</param>
        /// <returns>File access instance.</returns>
        public static AzureBlobFileAccess CreateFromSignature(Uri containerUri, string signature)
        {
            var credentials = new AzureSasCredential(signature);
            var container = new BlobContainerClient(containerUri, credentials);
            return new AzureBlobFileAccess(container);
        }

        /// <summary>
        /// The concept of directory does not exist in Azure Blobs. The only way to have a new "directory" is
        /// to create a dummy blob there, that is not worthy.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public override Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc/>
        public override async Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            bool isRoot = string.IsNullOrEmpty(relativePath);
            relativePath = this.GetFullPath(relativePath);
            if (!isRoot && !relativePath.EndsWith(PathDelimiter))
            {
                relativePath += PathDelimiter;
            }

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: PathDelimiter, prefix: relativePath);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (item.IsPrefix)
                {
                    var directory = item.Prefix.Split(this.prefixSplit, StringSplitOptions.RemoveEmptyEntries).Last();
                    await this.DeleteDirectoryAsync(item.Prefix, cancellationToken).ConfigureAwait(false);
                }
                else if (item.IsBlob)
                {
                    await this.container.DeleteBlobAsync(item.Blob.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public override Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            return file.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<IEnumerable<string>> EnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
            => this.EnumerateItemsAuxAsync(relativePath, true, cancellationToken);

        /// <inheritdoc/>
        public override Task<IEnumerable<string>> EnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
            => this.EnumerateItemsAuxAsync(relativePath, false, cancellationToken);

        /// <inheritdoc/>
        public override async Task<bool> ExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return true;
            }

            relativePath = this.GetFullPath(relativePath);

            var pathParts = relativePath.Split(this.prefixSplit, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var parent = pathParts.Length > 1 ? string.Join(PathDelimiter, pathParts.Take(pathParts.Length - 1)) : pathParts[0];
            parent += PathDelimiter;
            var directory = pathParts.Last();

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: PathDelimiter, prefix: parent).ConfigureAwait(false);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (item.IsPrefix)
                {
                    var prefixName = item.Prefix.Split(this.prefixSplit, StringSplitOptions.RemoveEmptyEntries).Last();
                    if (prefixName == directory)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override async Task<bool> ExistsFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            bool exists = await file.ExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return exists;
        }

        /// <inheritdoc/>
        public override async Task<Stream> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            var contents = await file.DownloadContentAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return contents.Value.Content.ToStream();
        }

        /// <inheritdoc/>
        public override Task WriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var blob = this.container.GetBlobClient(relativePath);
            return blob.UploadAsync(stream, true, cancellationToken);
        }

        private async Task<IEnumerable<string>> EnumerateItemsAuxAsync(string relativePath, bool directoriesOnly, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var items = new List<string>();
            if (!string.IsNullOrEmpty(relativePath))
            {
                relativePath += PathDelimiter;
            }

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: PathDelimiter, prefix: relativePath);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (directoriesOnly && item.IsPrefix)
                {
                    var directory = item.Prefix.Split(this.prefixSplit, StringSplitOptions.RemoveEmptyEntries).Last();
                    items.Add(directory);
                }
                else if (!directoriesOnly && item.IsBlob)
                {
                    items.Add(item.Blob.Name);
                }
            }

            return items;
        }

        private string GetFullPath(string relativePath)
        {
            if (!string.IsNullOrEmpty(this.BaseDirectory))
            {
                relativePath = Path.Combine(this.BaseDirectory, relativePath);
            }

            return relativePath.Replace(@"\", PathDelimiter);
        }
    }
}
