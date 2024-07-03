using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FileAccess = Evergine.Xrv.Core.Storage.FileAccess;

namespace Evergine.Xrv.Core.Tests.Storage
{
    internal static class TestHelpers
    {
        public static StorageConfiguration GetTestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<TestsConfiguration>()
                .Build();

            StorageConfiguration storageConfig = configuration
                .GetSection(nameof(TestsConfiguration.Storage))
                .Get<StorageConfiguration>();

            return storageConfig;
        }

        public static async Task<string> CreateTestFileAsync(FileAccess fileAccess, string filePath, string contents = null)
        {
            var originalFileContents = contents ?? "This is a secret message!";
            byte[] messageData = Encoding.UTF8.GetBytes(originalFileContents);
            using (var stream = new MemoryStream(messageData))
            {
                await fileAccess.WriteFileAsync(filePath, stream);
            }

            return filePath;
        }

        public static async Task PrepareTestFileSystemAsync(
            FileAccess fileAccess,
            int numberOfDirectories,
            int numberOfFilesPerDirectory)
        {
            string filePath;

            for (int fileIndex = 0; fileIndex < numberOfFilesPerDirectory; fileIndex++)
            {
                filePath = $"file_{fileIndex}.txt";
                byte[] data = Encoding.UTF8.GetBytes(filePath);
                using (var stream = new MemoryStream(data))
                {
                    await fileAccess.WriteFileAsync(filePath, stream);
                }
            }

            for (int directoryIndex = 0; directoryIndex < numberOfDirectories; directoryIndex++)
            {
                string directoryName = $"directory_{directoryIndex}";
                await fileAccess.CreateDirectoryAsync(directoryName);

                for (int fileIndex = 0; fileIndex < numberOfFilesPerDirectory; fileIndex++)
                {
                    filePath = Path.Combine(directoryName, $"file_{fileIndex}.txt");
                    byte[] data = Encoding.UTF8.GetBytes(filePath);
                    using (var stream = new MemoryStream(data))
                    {
                        await fileAccess.WriteFileAsync(filePath, stream);
                    }
                }
            }
        }

        public static async Task<string[]> CreateTestFilesWithSizeAsync(FileAccess fileAccess, long[] fileSizes, int startIndex = 0)
        {
            var fileNames = new string[fileSizes.Length];

            for (int i = startIndex; i - startIndex < fileSizes.Length; i++)
            {
                var size = fileSizes[i];
                fileNames[i] = $"file_{i}.dat";
                await CreateSingleFilesWithSizeAsync(fileAccess, fileNames[i], size);
            }

            return fileNames;
        }

        public static async Task CreateSingleFilesWithSizeAsync(FileAccess fileAccess, string filePath, long size)
        {
            using (var stream = new MemoryStream(new byte[size]))
            {
                await fileAccess.WriteFileAsync(filePath, stream);
            }
        }
    }
}
