using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Storage;
using Evergine.Xrv.Core.Utils;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Storage
{
    public class ApplicationDataFileAccessShould : IAsyncLifetime
    {
        private readonly ApplicationDataFileAccess fileAccess;

        public ApplicationDataFileAccessShould()
        {
            string roothPath = Path.Combine(DeviceHelper.GetLocalApplicationFolderPath(), "tests");
            this.fileAccess = new ApplicationDataFileAccess(roothPath);
        }

        Task IAsyncLifetime.InitializeAsync() => this.fileAccess.ClearAsync();

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

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
            Assert.Equal(numberOfFilesPerDirectory, rootFolderFiles.Count());

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
        public async Task EnsureOnlyBaseDirectoryItemsAreEnumerated()
        {
            this.fileAccess.BaseDirectory = "base";

            await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder2/file1.txt");

            var files = await this.fileAccess.EnumerateFilesAsync();
            var directories = await this.fileAccess.EnumerateDirectoriesAsync();

            Assert.Equal(2, files.Count());
            Assert.Equal(2, directories.Count());
        }

        [Fact]
        public async Task EnsureOnlySpecificDirectoryItemsAreEnumerated()
        {
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "file1.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/folder/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "folder/folder2/file1.txt");

            var files = await this.fileAccess.EnumerateFilesAsync("folder");
            var directories = await this.fileAccess.EnumerateDirectoriesAsync("folder");

            Assert.Equal(2, files.Count());
            Assert.Equal(2, directories.Count());
        }

        [Fact]
        public async Task DeleteADirectoryInDepth()
        {
            const string directoryName = "todelete";
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "todelete1/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "todelete1/file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "todelete1/folder/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "todelete1/folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "todelete1/folder2/file1.txt");
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
        public async Task RetrieveFileMetadata()
        {
            this.fileAccess.BaseDirectory = "base";
            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            var targetFile = await fileAccess.GetFileItemAsync("file.txt");

            Assert.NotNull(targetFile);
            Assert.Equal("file.txt", targetFile.Name);
            Assert.Equal("file.txt", targetFile.Path);
            Assert.NotNull(targetFile.CreationTime);
            Assert.NotNull(targetFile.ModificationTime);
            Assert.NotNull(targetFile.Size);
        }

        [Fact]
        public async Task RetrieveFileItem()
        {
            this.fileAccess.BaseDirectory = "base";

            string filePath = await TestHelpers.CreateTestFileAsync(this.fileAccess, "file.txt");
            var file = await this.fileAccess.GetFileItemAsync(filePath);
            Assert.NotNull(file);
            Assert.True(file.CreationTime != null);
            Assert.True(file.ModificationTime != null);
            Assert.NotNull(file.Size);
            Assert.Equal("file.txt", file.Name);
            Assert.Equal("file.txt", file.Path);
        }

        [Fact]
        public async Task RetrieveFileItemInDeepFolder()
        {
            this.fileAccess.BaseDirectory = "base";

            const string filePath = "path/to/the/file.txt";
            await TestHelpers.CreateTestFileAsync(this.fileAccess, filePath);
            var file = await this.fileAccess.GetFileItemAsync(filePath);
            Assert.NotNull(file);
            Assert.True(file.CreationTime != null);
            Assert.True(file.ModificationTime != null);
            Assert.NotNull(file.Size);
            Assert.Equal("file.txt", file.Name);
            Assert.Equal(filePath, file.Path);
        }

        [Fact]
        public async Task CreateDirectoriesStructureBySingleCall()
        {
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "level1_1/level2/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "level1_2/level2/file.txt");

            var directories = await this.fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(2, directories.Count());

            for (int i = 1; i <= directories.Count(); i++)
            {
                var directory = directories.ElementAt(i - 1);
                Assert.Equal($"level1_{i}", directory.Path);
                var subDirectories = await this.fileAccess.EnumerateDirectoriesAsync(directory.Path);
                Assert.Single(subDirectories);
                Assert.Equal($"level1_{i}/level2", subDirectories.ElementAt(0).Path);
            }
        }

        [Fact]
        public async Task CreateDirectoriesStructureWithBasePathBySingleCall()
        {
            this.fileAccess.BaseDirectory = "base";
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "level1_1/level2/file.txt");
            await TestHelpers.CreateTestFileAsync(this.fileAccess, "level1_2/level2/file.txt");

            var directories = await this.fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(2, directories.Count());

            for (int i = 1; i <= directories.Count(); i++)
            {
                var directory = directories.ElementAt(i - 1);
                Assert.Equal($"level1_{i}", directory.Path);
                var subDirectories = await this.fileAccess.EnumerateDirectoriesAsync(directory.Path);
                Assert.Single(subDirectories);
                Assert.Equal($"level1_{i}/level2", subDirectories.ElementAt(0).Path);
            }
        }
    }
}