using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xrv.Core.Storage;
using Xunit;

namespace Xrv.Core.Tests.Storage
{
    public class ApplicationDataFileAccessShould
    {
        private const string TestDirectory = "test";
        private readonly ApplicationDataFileAccess fileAccess;

        public ApplicationDataFileAccessShould()
        {
            this.fileAccess = new ApplicationDataFileAccess()
            {
                BaseDirectory = TestDirectory,
            };
            this.CleanTestFolder();
        }

        [Fact]
        public async Task CreateADirectory()
        {
            const string directoryPath = "folder";
            await this.fileAccess.CreateDirectoryAsync(directoryPath);
            bool existsDirectory = await this.fileAccess.ExistsDirectoryAsync(directoryPath);
            Assert.True(existsDirectory);
        }

        [Fact]
        public async Task CheckThatCreatedFileExits()
        {
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            bool exists = await this.fileAccess.ExistsFileAsync(filePath);
            Assert.True(exists);
        }

        [Fact]
        public async Task ReadFileContents()
        {
            const string originalFileContents = "contents";
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt", originalFileContents);
            using (var stream = await this.fileAccess.GetFileAsync(filePath))
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var storedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(originalFileContents, storedMessage);
            }
        }

        [Fact]
        public async Task EnumerateDirectoryStructure()
        {
            const int numberOfDirectories = 5;
            const int numberOfFilesPerDirectory = 3;

            await TestHelpers.PrepareTestFileSystem(this.fileAccess, numberOfDirectories, numberOfFilesPerDirectory);

            var rootFolderFiles = await this.fileAccess.EnumerateFilesAsync();
            Assert.Single(rootFolderFiles);

            var rootFolderDirectories = await this.fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(numberOfDirectories, rootFolderDirectories.Count());

            foreach (var directory in rootFolderDirectories)
            {
                var directoryFiles = await this.fileAccess.EnumerateFilesAsync(directory);
                Assert.Equal(numberOfFilesPerDirectory, directoryFiles.Count());
            }
        }

        [Fact]
        public async Task DeleteAFile()
        {
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            await this.fileAccess.DeleteFileAsync(filePath);
            bool exists = await fileAccess.ExistsFileAsync(filePath);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteADirectory()
        {
            const string directoryName = "todelete";
            await this.fileAccess.CreateDirectoryAsync(directoryName);
            await this.fileAccess.DeleteDirectoryAsync(directoryName);

            bool exists = await fileAccess.ExistsDirectoryAsync(directoryName);
            Assert.False(exists);
        }

        private void CleanTestFolder()
        {
            var executingFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var testFolderPath = Path.Combine(executingFolderPath, TestDirectory);
            if (Directory.Exists(testFolderPath))
            {
                Directory.Delete(testFolderPath, true);
            }

            Directory.CreateDirectory(testFolderPath);
        }
    }
}