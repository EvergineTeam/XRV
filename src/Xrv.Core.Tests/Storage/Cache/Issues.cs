using System.Threading.Tasks;
using Xrv.Core.Storage.Cache;
using Xunit;

namespace Xrv.Core.Tests.Storage.Cache
{
    public class Issues
    {
        private const long CacheSizeLimit = 1024 * 5; // 5KB
        private readonly DiskCache diskCache;

        public Issues()
        {
            this.diskCache = new DiskCache("issues", new DefaultFileBlockSelector())
            {
                SizeLimit = CacheSizeLimit,
            };
        }

        [Fact]
        public async Task StreamLengthErrosOnGetFileForAzureFileShare()
        {
            var fileShare = AzureFileShareFileShould.CreateFileAccessFromAuthentitactionType(
                AzureFileShareFileShould.AuthenticationType.ConnectionString);
            await fileShare.ClearAsync();
            await fileShare.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.CreateTestFileAsync(fileShare, "test.txt", "contents here");

            fileShare.Cache = this.diskCache;
            await this.diskCache.InitializeAsync(true);
            await fileShare.GetFileAsync("test.txt");
        }
    }
}
