using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.Storage.Cache;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Storage
{
    public class FileAccessShould
    {
        private readonly MockFileAccess fileAccess;
        private readonly Mock<DiskCache> diskCache;

        public FileAccessShould()
        {
            this.diskCache = new Mock<DiskCache>("fileAccessTest");
            this.fileAccess = new MockFileAccess()
            {
                ThrowException = true,
                Cache = this.diskCache.Object,
            };
        }

        [Fact]
        public async Task NotUseCacheIfNoExceptionIsThrown()
        {
            this.fileAccess.ThrowException = false;
            var items = await this.fileAccess.EnumerateDirectoriesAsync();
            this.diskCache
                .Verify(
                    cache => cache.EnumerateDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Never);
        }

        [Fact]
        public async Task UseCacheToEnumerateDirectories()
        {
            var items = await this.fileAccess.EnumerateDirectoriesAsync();
            this.diskCache
                .Verify(
                    cache => cache.EnumerateDirectoriesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task UseCacheToEnumerateFiles()
        {
            var items = await this.fileAccess.EnumerateFilesAsync();
            this.diskCache
                .Verify(
                    cache => cache.EnumerateFilesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task UseCacheGettingFiles()
        {
            const string fileName = "test.txt";

            this.diskCache
                .Setup(cache => cache.GetFileAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(new byte[1024]));

            await TestHelpers.CreateTestFileAsync(this.fileAccess, fileName);
            await this.fileAccess.GetFileAsync(fileName);
            this.diskCache
                .Verify(
                    cache => cache.WriteFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task ClearItsContents()
        {
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "test.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "test2.txt");
            await this.fileAccess.CreateDirectoryAsync("directory");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "directory/test.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "directory/test2.txt");
            await this.fileAccess.ClearAsync();

            var directories = await this.fileAccess.EnumerateDirectoriesAsync();
            var files = await this.fileAccess.EnumerateFilesAsync();

            Assert.Empty(directories);
            Assert.Empty(files);
        }

        [Fact]
        public async Task NotSaveFileToCacheIfDownloadIsNotCompleted()
        {
            bool fileSaved = false;
            this.diskCache
                .Setup(cache => cache.WriteFileAsync(
                    MockFileAccess.IncorrectDownloadFilePath,
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>()))
                .Callback(() => fileSaved = true);
            this.diskCache
                .Setup(cache => cache.GetFileAsync(
                    MockFileAccess.IncorrectDownloadFilePath,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MemoryStream(new byte[512]));
            this.diskCache
                .Setup(cache => cache.DeleteFileAsync(
                    MockFileAccess.IncorrectDownloadFilePath,
                    It.IsAny<CancellationToken>()))
                .Callback(() => fileSaved = false);
            this.diskCache
                .Setup(cache => cache.ExistsFileAsync(
                    MockFileAccess.IncorrectDownloadFilePath,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => fileSaved);

            bool fileIntegrityCheckFailed = false;
            try
            {
                await this.fileAccess.GetFileAsync(MockFileAccess.IncorrectDownloadFilePath);
            }
            catch (FileIntegrityException)
            {
                fileIntegrityCheckFailed = true;
            }

            Assert.True(fileIntegrityCheckFailed);

            bool existsFile = await this.fileAccess.Cache.ExistsFileAsync(MockFileAccess.IncorrectDownloadFilePath);
            Assert.False(existsFile);
        }

        private class MockFileAccess : ApplicationDataFileAccess
        {
            public const string IncorrectDownloadFilePath = "incorrect_download.bin";

            public bool ThrowException { get; set; } = false;

            public override Task<FileItem> GetFileItemAsync(string relativePath, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new FileItem(relativePath)
                {
                    Size = 1024,
                });
            }

            protected override Task<FileItem> InternalGetFileItemAsync(string relativePath, CancellationToken cancellationToken = default)
            {
                FileItem item = new FileItem(relativePath) { Size = 1024 };
                return Task.FromResult(item);
            }

            protected override async Task<Stream> InternalGetFileAsync(string relativePath, CancellationToken cancellationToken = default)
            {
                var itemData = await this.GetFileItemAsync(relativePath, cancellationToken).ConfigureAwait(false);
                Stream stream = null;

                if (relativePath == IncorrectDownloadFilePath)
                {
                    stream = new MemoryStream(new byte[(int)itemData.Size / 2]);
                }
                else
                {
                    stream = new MemoryStream(new byte[(int)itemData.Size]);
                }

                return stream;
            }

            protected override Task<IEnumerable<DirectoryItem>> InternalEnumerateDirectoriesAsync(string relativePath, CancellationToken cancellationToken = default)
            {
                this.CheckAndThrow();
                return base.InternalEnumerateDirectoriesAsync(relativePath, cancellationToken);
            }

            protected override Task<IEnumerable<FileItem>> InternalEnumerateFilesAsync(string relativePath, CancellationToken cancellationToken = default)
            {
                this.CheckAndThrow();
                return base.InternalEnumerateFilesAsync(relativePath, cancellationToken);
            }

            protected override bool InternalEvaluateCacheUsageOnException(Exception exception) =>
                exception is MyTestHttpConnectionErrorException;

            private void CheckAndThrow()
            {
                if (this.ThrowException)
                {
                    throw new MyTestHttpConnectionErrorException();
                }
            }
        }

        private class MyTestHttpConnectionErrorException : Exception
        {
        }
    }
}
