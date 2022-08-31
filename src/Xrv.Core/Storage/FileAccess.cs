// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xrv.Core.Storage
{
    /// <summary>
    /// Provides access to files and directories.
    /// </summary>
    public abstract class FileAccess
    {
        /// <summary>
        /// Enumerates directory from root directory.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of directories under root directory, first level only.</returns>
        public Task<IEnumerable<string>> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default) =>
            this.EnumerateDirectoriesAsync(string.Empty, cancellationToken);

        /// <summary>
        /// Enumerates directory from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children directories, first level only.</returns>
        public abstract Task<IEnumerable<string>> EnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerates files from root directory.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of files under root directory, first level only.</returns>
        public Task<IEnumerable<string>> EnumerateFilesAsync(CancellationToken cancellationToken = default) =>
            this.EnumerateFilesAsync(string.Empty, cancellationToken);

        /// <summary>
        /// Enumerates files from a given directory.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of children files, first level only.</returns>
        public abstract Task<IEnumerable<string>> EnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that a directory exists.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if directory exits; false otherwise.</returns>
        public abstract Task<bool> ExistsDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks that a file exists.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if file exits; false otherwise.</returns>
        public abstract Task<bool> ExistsFileAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a directory if it does not exist. If a folder hierarchy is provided, any unexisting directory
        /// will be created.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public abstract Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or overrides a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="stream">File contents stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public abstract Task WriteFileAsync(string relativePath, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a file contents.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>File contents stream.</returns>
        public abstract Task<Stream> GetFileAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a directory, if exits.
        /// </summary>
        /// <param name="relativePath">Directory path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public abstract Task DeleteDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file, if exits.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        public abstract Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
