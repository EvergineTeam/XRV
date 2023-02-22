// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Storage
{
    /// <summary>
    /// File access for Azure Blobs.
    /// </summary>
    public class AzureBlobFileAccess : FileAccess
    {
        /*
         * Note: Please, remember that in Azure Storage Blobs, there is no concept for directories.
         */

        private readonly BlobContainerClient container;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileAccess"/> class.
        /// </summary>
        /// <param name="container">Blob container client.</param>
        public AzureBlobFileAccess(BlobContainerClient container)
        {
            this.container = container;
        }

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

        /// <inheritdoc/>
        protected override Task InternalCreateBaseDirectoryIfNotExistsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <summary>
        /// The concept of directory does not exist in Azure Blobs. The only way to have a new "directory" is
        /// to create a dummy blob there, that is not worthy.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        protected override Task InternalCreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        /// <inheritdoc/>
        protected override async Task InternalDeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            bool isRoot = string.IsNullOrEmpty(relativePath);
            relativePath = this.GetFullPath(relativePath);
            if (!isRoot && !relativePath.EndsWith(DirectoryItem.PathSeparator))
            {
                relativePath += DirectoryItem.PathSeparator;
            }

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: DirectoryItem.PathSeparator, prefix: relativePath).ConfigureAwait(false);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (item.IsPrefix)
                {
                    await this.DeleteDirectoryAsync(item.Prefix, cancellationToken).ConfigureAwait(false);
                }
                else if (item.IsBlob)
                {
                    await this.container.DeleteBlobAsync(item.Blob.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        protected override Task InternalDeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            return file.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<DirectoryItem>> InternalEnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var items = await this.EnumerateItemsAuxAsync(relativePath, true, cancellationToken).ConfigureAwait(false);
            return items.Select(item => ConvertToDirectoryItem(item, relativePath));
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<FileItem>> InternalEnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var items = await this.EnumerateItemsAuxAsync(relativePath, false, cancellationToken).ConfigureAwait(false);
            return items.Select(item => ConvertToFileItem(item.Blob, string.IsNullOrEmpty(relativePath) ? relativePath : Path.GetDirectoryName(relativePath)));
        }

        /// <inheritdoc/>
        protected override async Task<bool> InternalExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return true;
            }

            relativePath = this.GetFullPath(relativePath);

            var pathParts = relativePath.Split(DirectoryItem.SplitSeparators, StringSplitOptions.RemoveEmptyEntries).ToArray();
            var parent = pathParts.Length > 1 ? string.Join(DirectoryItem.PathSeparator, pathParts.Take(pathParts.Length - 1)) : pathParts[0];
            parent += DirectoryItem.PathSeparator;
            var directory = pathParts.Last();

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: DirectoryItem.PathSeparator, prefix: parent).ConfigureAwait(false);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (item.IsPrefix)
                {
                    var prefixName = item.Prefix.Split(DirectoryItem.SplitSeparators, StringSplitOptions.RemoveEmptyEntries).Last();
                    if (prefixName == directory)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        protected override async Task<bool> InternalExistsFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            bool exists = await file.ExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return exists;
        }

        /// <inheritdoc/>
        protected override async Task<Stream> InternalGetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var file = this.container.GetBlobClient(relativePath);
            var contents = await file.DownloadContentAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return contents.Value.Content.ToStream();
        }

        /// <inheritdoc/>
        protected override Task InternalWriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var blob = this.container.GetBlobClient(relativePath);
            return blob.UploadAsync(stream, true, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task<FileItem> InternalGetFileItemAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);
            var blob = this.container.GetBlobClient(relativePath);
            var properties = await blob.GetPropertiesAsync();
            var fileItem = ConvertToFileItem(Path.GetFileName(relativePath), properties);
            return fileItem;
        }

        /// <inheritdoc/>
        protected override bool InternalEvaluateCacheUsageOnException(Exception exception) =>
            AzureCommon.EvaluateCacheUsageOnException(exception);

        private async Task<IEnumerable<BlobHierarchyItem>> EnumerateItemsAuxAsync(string relativePath, bool directoriesOnly, CancellationToken cancellationToken = default)
        {
            relativePath = this.GetFullPath(relativePath);

            var items = new List<BlobHierarchyItem>();
            if (!string.IsNullOrEmpty(relativePath))
            {
                relativePath += DirectoryItem.PathSeparator;
            }

            var pageable = this.container.GetBlobsByHierarchyAsync(delimiter: DirectoryItem.PathSeparator, prefix: relativePath);
            var enumerator = pageable.GetAsyncEnumerator();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var item = enumerator.Current;
                if (directoriesOnly && item.IsPrefix)
                {
                    items.Add(item);
                }
                else if (!directoriesOnly && item.IsBlob)
                {
                    items.Add(item);
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

            return relativePath.FixSlashes();
        }

        private static DirectoryItem ConvertToDirectoryItem(BlobHierarchyItem item, string basePath)
        {
            return new DirectoryItem(item.Prefix);
        }

        private static FileItem ConvertToFileItem(BlobItem item, string basePath) =>
            new FileItem(Path.Combine(basePath ?? string.Empty, item.Name))
            {
                CreationTime = item.Properties.CreatedOn?.UtcDateTime,
                ModificationTime = item.Properties.LastModified?.UtcDateTime,
                Size = item.Properties.ContentLength,
                Md5Hash = item.Properties.ContentHash,
            };

        private static FileItem ConvertToFileItem(string blobPath, BlobProperties properties) =>
            new FileItem(blobPath)
            {
                CreationTime = properties.CreatedOn.UtcDateTime,
                ModificationTime = properties.LastModified.UtcDateTime,
                Size = properties.ContentLength,
                Md5Hash = properties.ContentHash,
            };
    }
}
