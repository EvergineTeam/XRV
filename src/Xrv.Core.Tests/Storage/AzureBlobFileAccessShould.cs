using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xrv.Core.Storage;
using Xunit;

namespace Xrv.Core.Tests.Storage
{
    public class AzureBlobFileAccessShould : IAsyncLifetime
    {
        Task IAsyncLifetime.InitializeAsync()
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(AuthenticationType.ConnectionString);
            return fileAccess.ClearAsync();
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
            await TestHelpers.PrepareTestFileSystemAsync(fileAccess, numberOfDirectories, numberOfFilesPerDirectory);

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
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete/file.txt");
            await fileAccess.DeleteDirectoryAsync(directoryName);

            bool exists = await fileAccess.ExistsDirectoryAsync(directoryName);
            Assert.False(exists);
        }


        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task RetrieveFileMetadata(AuthenticationType type)
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            var files = await fileAccess.EnumerateFilesAsync();
            Assert.True(files.All(file => file.CreationTime != null));
            Assert.True(files.All(file => file.ModificationTime != null));
            Assert.True(files.All(file => file.Size != null));
            Assert.True(files.All(file => file.HasMd5Hash));
        }

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task RetrieveFileItem(AuthenticationType type)
        {
            var fileAccess = this.CreateFileAccessFromAuthentitactionType(type);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            var file = await fileAccess.GetFileItemAsync(filePath);
            Assert.NotNull(file);
            Assert.True(file.CreationTime != null);
            Assert.True(file.ModificationTime != null);
            Assert.NotNull(file.Size);
        }

        private AzureBlobFileAccess CreateFileAccessFromAuthentitactionType(AuthenticationType type)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetSection("Storage")
                .Get<StorageConfiguration>();

            AzureBlobFileAccess fileAccess = null;

            switch (type)
            {
                case AuthenticationType.ConnectionString:
                    fileAccess = AzureBlobFileAccess.CreateFromConnectionString(
                        configuration.AzureStorage.AccountConnectionString, 
                        configuration.AzureStorage.Blobs.ContainerName);
                    break;
                case AuthenticationType.Uri:
                    fileAccess = AzureBlobFileAccess.CreateFromUri(new Uri(configuration.AzureStorage.Blobs.UriWithSas));
                    break;
                case AuthenticationType.Signature:
                    fileAccess = AzureBlobFileAccess.CreateFromSignature(
                        new Uri(configuration.AzureStorage.Blobs.UriWithoutSas),
                        configuration.AzureStorage.Blobs.Sas);
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
