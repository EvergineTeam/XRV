using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xrv.Core.Storage;
using Xrv.Core.Storage.Cache;
using Xunit;

namespace Xrv.Core.Tests.Storage.Cache
{
    public class DiskCacheWithFileSelectorShould : IAsyncLifetime
    {
        private const long CacheSizeLimit = 1024 * 5; // 5KB
        private readonly DiskCache diskCache;

        public DiskCacheWithFileSelectorShould()
        {
            this.diskCache = new DiskCache("by-file", new DefaultFileBlockSelector())
            {
                SizeLimit = CacheSizeLimit,
            };
        }

        Task IAsyncLifetime.InitializeAsync() => this.diskCache.ClearAsync();

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task RegisterAddedCacheElements()
        {
            string[] fileNames = await TestHelpers.CreateTestFilesWithSizeAsync(
                this.diskCache,
                new[] { CacheSizeLimit / 4, CacheSizeLimit / 4, CacheSizeLimit / 4 });
            Assert.Equal(fileNames.Length, this.diskCache.CacheEntries.Count);
            Assert.Equal(CacheSizeLimit / 4, this.diskCache.CacheEntries.First().Value.DiskSize);
            Assert.Equal(CacheSizeLimit * 3 / 4, this.diskCache.CurrentCacheSize);
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[0]));
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[1]));
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[2]));
        }

        [Fact]
        public async Task RemoveOldEntriesIfSizeLimitHasBeenExceeded()
        {
            string[] fileNames = await TestHelpers.CreateTestFilesWithSizeAsync(
                this.diskCache,
                new[] { CacheSizeLimit / 2, CacheSizeLimit / 2, CacheSizeLimit / 2 });
            Assert.False(await this.diskCache.ExistsFileAsync(fileNames[0]));
            Assert.False(this.diskCache.CacheEntries.ContainsKey(fileNames[0]));
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[1]));
            Assert.True(this.diskCache.CacheEntries.ContainsKey(fileNames[1]));
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[2]));
            Assert.True(this.diskCache.CacheEntries.ContainsKey(fileNames[2]));
        }

        [Fact]
        public async Task ConsiderLastAccessDateTimeOnFlush()
        {
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file1.dat", CacheSizeLimit / 3);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file2.dat", CacheSizeLimit / 4);
            var stream = await this.diskCache.GetFileAsync("file1.dat");
            stream.Dispose();
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file3.dat", CacheSizeLimit / 2);

            Assert.True(await this.diskCache.ExistsFileAsync("file1.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file1.dat"));
            Assert.False(await this.diskCache.ExistsFileAsync("file2.dat"));
            Assert.False(this.diskCache.CacheEntries.ContainsKey("file2.dat"));
            Assert.True(await this.diskCache.ExistsFileAsync("file3.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file3.dat"));
        }

        [Fact]
        public async Task NotRemoveAddedItemEvenThoughLimitIsExceeded()
        {
            string[] fileNames = await TestHelpers.CreateTestFilesWithSizeAsync(
                this.diskCache,
                new[] { (long)(CacheSizeLimit * 1.1f) });
            Assert.True(await this.diskCache.ExistsFileAsync(fileNames[0]));
            Assert.True(this.diskCache.CacheEntries.ContainsKey(fileNames[0]));
        }

        [Fact]
        public async Task RemoveItemOnExpiredSlicing()
        {
            this.diskCache.SlidingExpiration = TimeSpan.FromMilliseconds(30);
            string[] fileNames = await TestHelpers.CreateTestFilesWithSizeAsync(
                this.diskCache,
                new[] { CacheSizeLimit / 2 });
            await Task.Delay(60);
            await this.diskCache.FlushAsync();
            Assert.False(await this.diskCache.ExistsFileAsync(fileNames[0]));
        }

        [Fact]
        public async Task UpdatesLastAccessWhenAFileIsRetrieved()
        {
            string[] fileNames = await TestHelpers.CreateTestFilesWithSizeAsync(
                this.diskCache,
                new[] { CacheSizeLimit / 2 });
            DateTime? initialAccess = this.diskCache.CacheEntries.First().Value.LastAccess;

            var stream = await this.diskCache.GetFileAsync(fileNames[0]);
            stream.Dispose();
            DateTime? currentAccess = this.diskCache.CacheEntries.First().Value.LastAccess;

            Assert.NotEqual(initialAccess, currentAccess);
        }

        [Fact]
        public async Task AllowWritingSameFileTwice()
        {
            await TestHelpers.CreateTestFilesWithSizeAsync(this.diskCache, new[] { CacheSizeLimit / 3 });
            // this overwrittes single file with new contents
            await TestHelpers.CreateTestFilesWithSizeAsync(this.diskCache, new[] { CacheSizeLimit / 2 });
            Assert.Equal(CacheSizeLimit / 2, this.diskCache.CacheEntries.First().Value.DiskSize);
        }

        [Fact]
        public async Task ThreadSafeUpdateCacheEntries()
        {
            const int NumberOfConcurrentFiles = 10;

            var tasks = new List<Task>();
            for (int i = 0; i < NumberOfConcurrentFiles; i++)
            {
                var current = i;
                var t = Task.Run(async () =>
                {
                    await TestHelpers.CreateSingleFilesWithSizeAsync(
                        this.diskCache,
                        $"file_{current}.dat",
                        CacheSizeLimit / (NumberOfConcurrentFiles + 1));
                });

                tasks.Add(t);
            }

            await Task.WhenAll(tasks);

            Assert.Equal(NumberOfConcurrentFiles, this.diskCache.CacheEntries.Count);
        }

        [Fact]
        public async Task FlushDirectoryContentsWhenInitialized()
        {
            var fileAccess = new ApplicationDataFileAccess
            {
                BaseDirectory = this.diskCache.BaseDirectory,
            };

            await TestHelpers.CreateTestFilesWithSizeAsync(fileAccess, new[] { CacheSizeLimit / 3, CacheSizeLimit / 2, CacheSizeLimit / 2 });
            await this.diskCache.InitializeAsync(true);

            Assert.Equal(2, this.diskCache.CacheEntries.Count);
        }

        [Fact]
        public async Task ConsiderPreviousLastAccessOnInit()
        {
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file1.dat", CacheSizeLimit / 3);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file2.dat", CacheSizeLimit / 3);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file3.dat", CacheSizeLimit / 3);
            var stream = await this.diskCache.GetFileAsync("file1.dat");
            stream.Dispose();

            diskCache.SizeLimit = (long)(CacheSizeLimit * 0.8f);
            await this.diskCache.InitializeAsync(true);

            Assert.True(await this.diskCache.ExistsFileAsync("file1.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file1.dat"));
            Assert.False(await this.diskCache.ExistsFileAsync("file2.dat"));
            Assert.False(this.diskCache.CacheEntries.ContainsKey("file2.dat"));
            Assert.True(await this.diskCache.ExistsFileAsync("file3.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file3.dat"));
        }

        [Fact]
        public async Task NotConsiderCacheFile()
        {
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file1.dat", CacheSizeLimit / 3);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file2.dat", CacheSizeLimit / 4);
            await this.diskCache.InitializeAsync(true);

            var files = await this.diskCache.EnumerateFilesAsync();

            Assert.Equal(2, this.diskCache.CacheEntries.Count);
            Assert.True(files.All(file => file.Name != DiskCache.CacheStatusFileName));
        }

        [Fact]
        public async Task IgnoreCacheStatusEntriesThatActuallyNotExist()
        {
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file1.dat", CacheSizeLimit / 3);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file2.dat", CacheSizeLimit / 4);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "file3.dat", CacheSizeLimit / 4);

            var fileAccess = new ApplicationDataFileAccess()
            {
                BaseDirectory = this.diskCache.BaseDirectory,
            };
            await fileAccess.DeleteFileAsync("file2.dat");
            await this.diskCache.InitializeAsync(true);

            Assert.Equal(2, this.diskCache.CacheEntries.Count);
            Assert.True(await this.diskCache.ExistsFileAsync("file1.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file1.dat"));
            Assert.False(await this.diskCache.ExistsFileAsync("file2.dat"));
            Assert.False(this.diskCache.CacheEntries.ContainsKey("file2.dat"));
            Assert.True(await this.diskCache.ExistsFileAsync("file3.dat"));
            Assert.True(this.diskCache.CacheEntries.ContainsKey("file3.dat"));
        }
    }
}
