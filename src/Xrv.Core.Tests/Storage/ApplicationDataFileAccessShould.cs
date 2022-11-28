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

            await TestHelpers.PrepareTestFileSystemAsync(this.fileAccess, numberOfDirectories, numberOfFilesPerDirectory);

            var rootFolderFiles = await this.fileAccess.EnumerateFilesAsync();
            Assert.Single(rootFolderFiles);

            var rootFolderDirectories = await this.fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(numberOfDirectories, rootFolderDirectories.Count());

            foreach (var directory in rootFolderDirectories)
            {
                var directoryFiles = await this.fileAccess.EnumerateFilesAsync(directory.Name);
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

        [Fact]
        public async Task RetrieveDirectoryDates()
        {
            const string directoryName = "dates";
            await this.fileAccess.CreateDirectoryAsync(directoryName);

            var directories = await this.fileAccess.EnumerateDirectoriesAsync();
            Assert.True(directories.All(directory => directory.CreationTime != null));
            Assert.True(directories.All(directory => directory.ModificationTime != null));
        }

        [Fact]
        public async Task RetrieveFileDates()
        {
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            var files = await this.fileAccess.EnumerateFilesAsync();
            Assert.True(files.All(file => file.CreationTime != null));
            Assert.True(files.All(file => file.ModificationTime != null));
        }

        [Fact]
        public async Task RetrieveFileItem()
        {
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            var file = await this.fileAccess.GetFileItemAsync(filePath);
            Assert.NotNull(file);
            Assert.True(file.CreationTime != null);
            Assert.True(file.ModificationTime != null);
            Assert.NotNull(file.Size);
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