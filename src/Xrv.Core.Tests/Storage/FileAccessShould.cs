using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xrv.Core.Storage;
using Xrv.Core.Storage.Cache;
using Xunit;

namespace Xrv.Core.Tests.Storage
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

        private class MockFileAccess : ApplicationDataFileAccess
        {
            public bool ThrowException { get; set; } = false;

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
