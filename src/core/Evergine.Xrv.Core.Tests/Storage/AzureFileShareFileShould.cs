﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Storage;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Storage
{
    [Trait("Category", "Integration")]
    public class AzureFileShareFileShould : IAsyncLifetime
    {
        Task IAsyncLifetime.InitializeAsync()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.ConnectionString);
            return fileAccess.ClearAsync();
        }

        Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

        [Theory]
        [InlineData(AuthenticationType.ConnectionString)]
        [InlineData(AuthenticationType.Uri)]
        [InlineData(AuthenticationType.Signature)]
        public async Task CheckThatCreatedFileExits(AuthenticationType type)
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(type);
            var filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            bool exists = await fileAccess.ExistsFileAsync(filePath);
            Assert.True(exists);
        }

        [Fact]
        public async Task ReadFileContents()
        {
            const string originalFileContents = "contents";
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt", originalFileContents);
            using (var stream = await fileAccess.GetFileAsync(filePath))
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

            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);

            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.PrepareTestFileSystemAsync(fileAccess, numberOfDirectories, numberOfFilesPerDirectory);

            var rootFolderFiles = await fileAccess.EnumerateFilesAsync();
            Assert.Equal(numberOfFilesPerDirectory, rootFolderFiles.Count());

            var rootFolderDirectories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(numberOfDirectories, rootFolderDirectories.Count());

            foreach (var directory in rootFolderDirectories)
            {
                var directoryFiles = await fileAccess.EnumerateFilesAsync(directory.Name);
                Assert.Equal(numberOfFilesPerDirectory, directoryFiles.Count());
            }
        }

        [Fact]
        public async Task DeleteAFile()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            await fileAccess.DeleteFileAsync(filePath);
            bool exists = await fileAccess.ExistsFileAsync(filePath);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteADirectory()
        {
            const string directoryName = "todelete";
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            await fileAccess.CreateDirectoryAsync(directoryName);
            await fileAccess.DeleteDirectoryAsync(directoryName);

            bool exists = await fileAccess.ExistsDirectoryAsync(directoryName);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteADirectoryInDepth()
        {
            const string directoryName = "todelete";
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete1/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete1/file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete1/folder/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete1/folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "todelete1/folder2/file1.txt");
            await fileAccess.DeleteDirectoryAsync(directoryName);

            bool exists = await fileAccess.ExistsDirectoryAsync(directoryName);
            Assert.False(exists);
        }

        [Fact]
        public async Task EnsureOnlyBaseDirectoryItemsAreEnumerated()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            fileAccess.BaseDirectory = "base";

            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder2/file1.txt");

            var files = await fileAccess.EnumerateFilesAsync();
            var directories = await fileAccess.EnumerateDirectoriesAsync();

            Assert.Equal(2, files.Count());
            Assert.Equal(2, directories.Count());
        }

        [Fact]
        public async Task EnsureOnlySpecificDirectoryItemsAreEnumerated()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            await TestHelpers.CreateTestFileAsync(fileAccess, "file1.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/folder/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/folder/file2.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "folder/folder2/file1.txt");

            var files = await fileAccess.EnumerateFilesAsync("folder");
            var directories = await fileAccess.EnumerateDirectoriesAsync("folder");

            Assert.Equal(2, files.Count());
            Assert.Equal(2, directories.Count());
        }

        [Fact]
        public async Task RetrieveDirectoryDates()
        {
            const string directoryName = "dates";
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            await fileAccess.CreateDirectoryAsync(directoryName);

            var directories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.True(directories.All(directory => directory.CreationTime != null));
            Assert.True(directories.All(directory => directory.ModificationTime != null));
        }

        [Fact]
        public async Task RetrieveFileMetadata()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            fileAccess.BaseDirectory = "base";

            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
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
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            fileAccess.BaseDirectory = "base";

            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            string filePath = await TestHelpers.CreateTestFileAsync(fileAccess, "file.txt");
            var file = await fileAccess.GetFileItemAsync(filePath);

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
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            fileAccess.BaseDirectory = "base";

            const string filePath = "path/to/the/file.txt";
            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.CreateTestFileAsync(fileAccess, filePath);
            var file = await fileAccess.GetFileItemAsync(filePath);
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
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            await TestHelpers.CreateTestFileAsync(fileAccess, "level1_1/level2/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "level1_2/level2/file.txt");

            var directories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(2, directories.Count());

            for (int i = 1; i <= directories.Count(); i++)
            {
                var directory = directories.ElementAt(i - 1);
                Assert.Equal($"level1_{i}", directory.Path);
                var subDirectories = await fileAccess.EnumerateDirectoriesAsync(directory.Path);
                Assert.Single(subDirectories);
                Assert.Equal($"level1_{i}/level2", subDirectories.ElementAt(0).Path);
            }
        }

        [Fact]
        public async Task CreateDirectoriesStructureWithBasePathBySingleCall()
        {
            var fileAccess = CreateFileAccessFromAuthentitactionType(AuthenticationType.Uri);
            fileAccess.BaseDirectory = "base";
            await fileAccess.CreateBaseDirectoryIfNotExistsAsync();
            await TestHelpers.CreateTestFileAsync(fileAccess, "level1_1/level2/file.txt");
            await TestHelpers.CreateTestFileAsync(fileAccess, "level1_2/level2/file.txt");

            var directories = await fileAccess.EnumerateDirectoriesAsync();
            Assert.Equal(2, directories.Count());

            for (int i = 1; i <= directories.Count(); i++)
            {
                var directory = directories.ElementAt(i - 1);
                Assert.Equal($"level1_{i}", directory.Path);
                var subDirectories = await fileAccess.EnumerateDirectoriesAsync(directory.Path);
                Assert.Single(subDirectories);
                Assert.Equal($"level1_{i}/level2", subDirectories.ElementAt(0).Path);
            }
        }

        internal static AzureFileShareFileAccess CreateFileAccessFromAuthentitactionType(AuthenticationType type)
        {
            StorageConfiguration storageConfig = TestHelpers.GetTestConfiguration();
            AzureFileShareFileAccess fileAccess = null;

            switch (type)
            {
                case AuthenticationType.ConnectionString:
                    fileAccess = AzureFileShareFileAccess.CreateFromConnectionString(
                        storageConfig.AzureStorage.AccountConnectionString,
                        storageConfig.AzureStorage.FileShare.ShareName);
                    break;
                case AuthenticationType.Uri:
                    fileAccess = AzureFileShareFileAccess.CreateFromUri(new Uri(storageConfig.AzureStorage.FileShare.UriWithSas));
                    break;
                case AuthenticationType.Signature:
                    fileAccess = AzureFileShareFileAccess.CreateFromSignature(
                        new Uri(storageConfig.AzureStorage.FileShare.UriWithoutSas),
                        storageConfig.AzureStorage.FileShare.Sas);
                    break;
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
