using System.Threading.Tasks;
using Xrv.Core.Storage.Cache;
using Xunit;

namespace Xrv.Core.Tests.Storage.Cache
{
    public class DiskCacheWithDirectorySelectorShould : IAsyncLifetime
    {
        private const long CacheSizeLimit = 1024 * 5; // 5KB
        private readonly DiskCache diskCache;

        public DiskCacheWithDirectorySelectorShould()
        {
            this.diskCache = new DiskCache("by-directory", new DefaultDirectoryBlockSelector())
            {
                SizeLimit = CacheSizeLimit,
            };
        }

        Task IAsyncLifetime.InitializeAsync() => this.diskCache.ClearAsync();

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task ConsiderFullFolderAsCacheBlock()
        {
            await this.diskCache.CreateDirectoryAsync("directory1");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-1.dat", CacheSizeLimit / 4);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-2.dat", CacheSizeLimit / 4);

            await this.diskCache.CreateDirectoryAsync("directory2");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory2/file2-1.dat", CacheSizeLimit / 4);

            Assert.Equal(2, this.diskCache.CacheEntries.Count);
            Assert.True(await this.diskCache.ExistsFileAsync("directory1/file1-1.dat"));
            Assert.True(await this.diskCache.ExistsFileAsync("directory1/file1-2.dat"));
            Assert.True(await this.diskCache.ExistsDirectoryAsync("directory1"));
        }

        [Fact]
        public async Task ReturnAccumulatedSizeForEachDirectory()
        {
            await this.diskCache.CreateDirectoryAsync("directory1");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-1.dat", CacheSizeLimit / 4);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-2.dat", CacheSizeLimit / 4);

            await this.diskCache.CreateDirectoryAsync("directory2");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory2/file2-1.dat", CacheSizeLimit / 4);

            Assert.Equal(CacheSizeLimit / 2, this.diskCache.CacheEntries["directory1"].DiskSize);
            Assert.Equal(CacheSizeLimit / 4, this.diskCache.CacheEntries["directory2"].DiskSize);
        }

        [Fact]
        public async Task RemoveLastRecentAccessedFolderWhenSizeLimitIsReached()
        {
            await this.diskCache.CreateDirectoryAsync("directory1");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-1.dat", CacheSizeLimit / 4);
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory1/file1-2.dat", CacheSizeLimit / 4);

            await this.diskCache.CreateDirectoryAsync("directory2");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory2/file2-1.dat", CacheSizeLimit / 4);

            await this.diskCache.CreateDirectoryAsync("directory3");
            await TestHelpers.CreateSingleFilesWithSizeAsync(this.diskCache, "directory3/file3-1.dat", CacheSizeLimit / 2);

            Assert.False(this.diskCache.CacheEntries.ContainsKey("directory1"));
            Assert.False(await this.diskCache.ExistsFileAsync("directory1/file1-1.dat"));
            Assert.False(await this.diskCache.ExistsFileAsync("directory1/file1-2.dat"));
            Assert.False(await this.diskCache.ExistsDirectoryAsync("directory1"));

            Assert.Equal(2, this.diskCache.CacheEntries.Count);
            Assert.Equal(CacheSizeLimit * 3 / 4, this.diskCache.CurrentCacheSize);
        }
    }
}
