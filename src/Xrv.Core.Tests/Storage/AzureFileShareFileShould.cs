using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using Xunit;
using Xrv.Core.Storage;
using System.Linq;

namespace Xrv.Core.Tests.Storage
{
    public class AzureFileShareFileShould : IAsyncLifetime
    {
        async Task IAsyncLifetime.InitializeAsync()
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(AuthenticationType.ConnectionString);
            var directories = await fileAccess.EnumerateDirectoriesAsync();
            foreach (var directory in directories)
            {
                await fileAccess.DeleteDirectoryAsync(directory.Name);
            }

            var files = await fileAccess.EnumerateFilesAsync();
            foreach (var file in files)
            {
                await fileAccess.DeleteFileAsync(file.Name);
            }
        }

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task CheckThatCreatedFileExits(AuthenticationType type)
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            var filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            bool exists = await fileAccess.ExistsFileAsync(filePath);
            Assert.True(exists);
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task ReadFileContents(AuthenticationType type)
        {
            const string originalFileContents = "contents";
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt", originalFileContents);
            using (var stream = await fileAccess.GetFileAsync(filePath))
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var storedMessage = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(originalFileContents, storedMessage);
            }
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task EnumerateDirectoryStructure(AuthenticationType type)
        {
            const int numberOfDirectories = 5;
            const int numberOfFilesPerDirectory = 3;

            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);

            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.PrepareTestFileSystem(fileAccess, numberOfDirectories, numberOfFilesPerDirectory);

            var rootFolderFiles = await fileAccess.EnumerateFilesAsync();
            Assert.Single(rootFolderFiles);

            var rootFolderDirectories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(numberOfDirectories, rootFolderDirectories.Count());

            foreach (var directory in rootFolderDirectories)
            {
                var directoryFiles = await fileAccess.EnumerateFilesAsync(directory.Name);
                Assert.Equal(numberOfFilesPerDirectory, directoryFiles.Count());
            }
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task DeleteAFile(AuthenticationType type)
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            await fileAccess.DeleteFileAsync(filePath);
            bool exists = await fileAccess.ExistsFileAsync(filePath);
            Assert.False(exists);
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task DeleteADirectory(AuthenticationType type)
        {
            const string directoryName = "todelete";
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            await fileAccess.CreateDirectoryAsync(directoryName);
            await fileAccess.DeleteDirectoryAsync(directoryName);

            bool exists = await fileAccess.ExistsDirectoryAsync(directoryName);
            Assert.False(exists);
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task RetrieveDirectoryDates(AuthenticationType type)
        {
            const string directoryName = "dates";
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            await fileAccess.CreateDirectoryAsync(directoryName);

            var directories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.True(directories.All(directory => directory.CreationTime != null));
            Assert.True(directories.All(directory => directory.ModificationTime != null));
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task RetrieveFileDates(AuthenticationType type)
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            var files = await fileAccess.EnumerateFilesAsync();
            Assert.True(files.All(file => file.CreationTime != null));
            Assert.True(files.All(file => file.ModificationTime != null));
        }

        private AzureFileShareFileAccess CreateFileAccessFromAuthentitactionType(AuthenticationType type)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("Storage")
                .Get<StorageConfiguration>();

            AzureFileShareFileAccess fileAccess = null;

            switch (type)
            {
                case AuthenticationType.ConnectionString:
                    fileAccess = AzureFileShareFileAccess.CreateFromConnectionString(
                        configuration.AzureStorage.AccountConnectionString,
                        configuration.AzureStorage.FileShare.ShareName);
                    break;
                case AuthenticationType.Uri:
                    fileAccess = AzureFileShareFileAccess.CreateFromUri(new Uri(configuration.AzureStorage.FileShare.UriWithSas));
                    break;
                case AuthenticationType.Signature:
                    fileAccess = AzureFileShareFileAccess.CreateFromSignature(
                        new Uri(configuration.AzureStorage.FileShare.UriWithoutSas),
                        configuration.AzureStorage.FileShare.Sas);
                    break;
            }

            if (fileAccess != null)
            {
                fileAccess.BaseDirectory = "base";
            }

            return fileAccess;
        }

        public enum AuthenticationType
        {
            ConnectionString,
            Uri,
            Signature,
        }
    }
}
