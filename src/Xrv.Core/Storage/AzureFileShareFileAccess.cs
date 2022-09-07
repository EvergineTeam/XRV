// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Azure;
using Azure.Storage.Files.Shares;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xrv.Core.Storage
{
    /// <summary>
    /// File access for Azure File Shares.
    /// </summary>
    public class AzureFileShareFileAccess : FileAccess
    {
        private readonly ShareClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFileShareFileAccess"/> class.
        /// </summary>
        /// <param name="client">Azure share client.</param>
        public AzureFileShareFileAccess(ShareClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Creates a file access instance from connection string and share name.
        /// </summary>
        /// <param name="connectionString">Azure storage account connection string.</param>
        /// <param name="shareName">File share name.</param>
        /// <returns>File access instance.</returns>
        public static AzureFileShareFileAccess CreateFromConnectionString(string connectionString, string shareName)
        {
            var client = new ShareClient(connectionString, shareName);
            return new AzureFileShareFileAccess(client);
        }

        /// <summary>
        /// Creates a file access instance from share URI, that should include SAS.
        /// </summary>
        /// <param name="shareUri">Share URI.</param>
        /// <returns>File access instance.</returns>
        public static AzureFileShareFileAccess CreateFromUri(Uri shareUri)
        {
            var sas = shareUri.Query.Substring(1, shareUri.Query.Length - 1);
            var credentials = new AzureSasCredential(sas);
            var builder = new UriBuilder
            {
                Host = shareUri.Host,
                Scheme = shareUri.Scheme,
                Path = shareUri.AbsolutePath,
            };
            var client = new ShareClient(builder.Uri, credentials);
            return new AzureFileShareFileAccess(client);
        }

        /// <summary>
        /// Creates a file access instance from share URI, that should not include SAS.
        /// </summary>
        /// <param name="shareUri">Share URI.</param>
        /// <param name="signature">SAS token.</param>
        /// <returns>File access instance.</returns>
        public static AzureFileShareFileAccess CreateFromSignature(Uri shareUri, string signature)
        {
            var credentials = new AzureSasCredential(signature);
            var client = new ShareClient(shareUri, credentials);
            return new AzureFileShareFileAccess(client);
        }

        /// <inheritdoc/>
        public override async Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            var directories = relativePath.Split(Path.DirectorySeparatorChar);
            var directory = this.client.GetRootDirectoryClient();

            for (int i = 0; i < directories.Length; i++)
            {
                string subDirectory = directories[i];
                directory = directory.GetSubdirectoryClient(subDirectory);
                await directory.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override async Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            ShareDirectoryClient directory = this.CreateDirectoryClient(relativePath);

            // Recursively delete directories, as SDK does not support it
            foreach (var subDirectoryName in await this.EnumerateDirectoriesAsync(directory.Path).ConfigureAwait(false))
            {
                await this.DeleteDirectoryAsync(Path.Combine(relativePath, subDirectoryName)).ConfigureAwait(false);
            }

            // Delete directory files
            foreach (var fileName in await this.EnumerateFilesAsync(directory.Path).ConfigureAwait(false))
            {
                var file = directory.GetFileClient(fileName);
                await file.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(directory.Path))
            {
                await directory.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            string directoryPath = Path.GetDirectoryName(relativePath);
            string fileName = Path.GetFileName(relativePath);
            ShareDirectoryClient directory = this.CreateDirectoryClient(directoryPath);
            ShareFileClient file = directory.GetFileClient(fileName);
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
            ShareDirectoryClient directory = this.CreateDirectoryClient(relativePath);
            bool exists = await directory.ExistsAsync(cancellationToken: cancellationToken);
            return exists;
        }

        /// <inheritdoc/>
        public override async Task<bool> ExistsFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            string directoryPath = Path.GetDirectoryName(relativePath);
            string fileName = Path.GetFileName(relativePath);
            ShareDirectoryClient directory = this.CreateDirectoryClient(directoryPath);
            ShareFileClient file = directory.GetFileClient(fileName);

            bool exists = await file.ExistsAsync(cancellationToken: cancellationToken);
            return exists;
        }

        /// <inheritdoc/>
        public override async Task<Stream> GetFileAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            string directoryPath = Path.GetDirectoryName(relativePath);
            string fileName = Path.GetFileName(relativePath);
            ShareDirectoryClient directory = this.CreateDirectoryClient(directoryPath);
            ShareFileClient file = directory.GetFileClient(fileName);
            var download = await file.DownloadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return download.Value.Content;
        }

        /// <inheritdoc/>
        public override async Task WriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default)
        {
            string directoryPath = Path.GetDirectoryName(relativePath);
            string fileName = Path.GetFileName(relativePath);
            ShareDirectoryClient directory = this.CreateDirectoryClient(directoryPath);
            ShareFileClient file = await directory
                .CreateFileAsync(fileName, stream.Length, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            await file.UploadAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<IEnumerable<string>> EnumerateItemsAuxAsync(string relativePath, bool directoriesOnly, CancellationToken cancellationToken = default)
        {
            ShareDirectoryClient directory = this.CreateDirectoryClient(relativePath);
            var enumerable = directory.GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken);
            var enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
            var items = new List<string>();

            while (!cancellationToken.IsCancellationRequested && await enumerator.MoveNextAsync())
            {
                var current = enumerator.Current;
                if (current.IsDirectory == directoriesOnly)
                {
                    items.Add(current.Name);
                }
            }

            return items;
        }

        private ShareDirectoryClient CreateDirectoryClient(string relativePath)
        {
            ShareDirectoryClient directoryClient = string.IsNullOrEmpty(relativePath)
                ? this.client.GetRootDirectoryClient()
                : this.client.GetDirectoryClient(relativePath);

            return directoryClient;
        }
    }
}
