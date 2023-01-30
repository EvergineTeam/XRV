using Evergine.Components.Fonts;
using Evergine.Framework.Threading;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Storage;
using FileAccess = Evergine.Xrv.Core.Storage.FileAccess;

namespace XrvSamples.Scenes
{
    internal class StorageScene : BaseScene
    {
        private const int StagesDelaySeconds = 2;
        private FileAccessConfiguration localApplicationDataConfig;
        private FileAccessConfiguration azureFileShareConfig;
        private FileAccessConfiguration azureBlobsConfig;

        public override void Initialize()
        {
            base.Initialize();

            var entityManager = this.Managers.EntityManager;

            /*
             * Local application data
             */
            this.localApplicationDataConfig = new FileAccessConfiguration();
            this.localApplicationDataConfig.ProgressText = entityManager.FindAllByTag("localAppData").First().FindComponentInChildren<Text3DMesh>();
            this.localApplicationDataConfig.FileAccess = new ApplicationDataFileAccess();
            var button = entityManager.FindAllByTag("localAppData").First().FindComponentInChildren<PressableButton>();
            button.ButtonReleased += this.LocalApplicationDataButton_ButtonReleased;

            /*
             * Azure file share
             */
            this.azureFileShareConfig = new FileAccessConfiguration();
            this.azureFileShareConfig.ProgressText = entityManager.FindAllByTag("azureFileShare").First().FindComponentInChildren<Text3DMesh>();
            FileAccess fileAccess = default;

            // Uncomment to check one of the auth modes
            //fileAccess = AzureFileShareFileAccess.CreateFromConnectionString("<REPLACE BY CONNECTION STRING>", "<REPLACE BY SHARE NAME>");
            //fileAccess = AzureFileShareFileAccess.CreateFromSignature(new Uri("<REPLACE BY SHARE URI>"), "<REPLACE BY SAS TOKEN>");
            //fileAccess = AzureFileShareFileAccess.CreateFromUri(new Uri("<REPLACE BY SHARE URI WITH SAS>"));

            this.azureFileShareConfig.FileAccess = fileAccess;
            button = entityManager.FindAllByTag("azureFileShare").First().FindComponentInChildren<PressableButton>();
            button.ButtonReleased += this.AzureFileShareButton_ButtonReleased;

            /*
             * Azure blobs
             */

            this.azureBlobsConfig = new FileAccessConfiguration();
            this.azureBlobsConfig.ProgressText = entityManager.FindAllByTag("azureBlobs").First().FindComponentInChildren<Text3DMesh>();
            fileAccess = default;

            // Uncomment to check one of the auth modes
            //fileAccess = AzureBlobFileAccess.CreateFromConnectionString("<REPLACE BY CONNECTION STRING>", "<REPLACE BY CONTAINER NAME>");
            //fileAccess = AzureBlobFileAccess.CreateFromSignature(new Uri("<REPLACE BY CONTAINER URI>"), "<REPLACE BY SAS TOKEN>");
            //fileAccess = AzureBlobFileAccess.CreateFromUri(new Uri("<REPLACE BY SHARE URI>"));

            this.azureBlobsConfig.FileAccess = fileAccess;
            button = entityManager.FindAllByTag("azureBlobs").First().FindComponentInChildren<PressableButton>();
            button.ButtonReleased += this.AzureBlobsButton_ButtonReleased;

            // Uncomment to test this in Android (not Quest) or force execution
            // EvergineForegroundTask.Run(async () => await this.ExecuteCaseAsync(this.localApplicationDataConfig));
            EvergineForegroundTask.Run(async () => await this.ExecuteCaseAsync(this.azureFileShareConfig));
            EvergineForegroundTask.Run(async () => await this.ExecuteCaseAsync(this.azureBlobsConfig));
        }

        private async void LocalApplicationDataButton_ButtonReleased(object sender, EventArgs e)
        {
            await this.ExecuteCaseAsync(this.localApplicationDataConfig);
        }

        private async void AzureFileShareButton_ButtonReleased(object sender, EventArgs e)
        {
            await this.ExecuteCaseAsync(this.azureFileShareConfig);
        }

        private async void AzureBlobsButton_ButtonReleased(object sender, EventArgs e)
        {
            await this.ExecuteCaseAsync(this.azureBlobsConfig);
        }

        private async Task ExecuteCaseAsync(FileAccessConfiguration test)
        {
            if (test.InProgress)
            {
                return;
            }

            try
            {
                test.InProgress = true;
                test.ProgressText.Text = $"Starting {test.FileAccess.GetType().Name}...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Enumerating directories ===========");
                var directories = await test.FileAccess.EnumerateDirectoriesAsync();
                foreach (var directory in directories)
                {
                    Debug.WriteLine(directory);
                }

                test.ProgressText.Text = $"There are {directories.Count()} directories in root folder...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Enumerating files ===========");
                var files = await test.FileAccess.EnumerateFilesAsync();
                foreach (var file in files)
                {
                    Debug.WriteLine(file);
                }

                test.ProgressText.Text = $"There are {files.Count()} files in root folder...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Creating directory and file ===========");
                const string fileName = "myfile.bin";
                var rootFolder = "test";
                var folderPath = Path.Combine(rootFolder, "myfolder");
                var filePath = Path.Combine(folderPath, fileName);
                await test.FileAccess.CreateDirectoryAsync(folderPath);

                test.ProgressText.Text = $"Created folder {folderPath}...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                var secretMessage = "This is a secret message!";
                byte[] messageData = UTF8Encoding.UTF8.GetBytes(secretMessage);
                using (var stream = new MemoryStream(messageData))
                {
                    await test.FileAccess.WriteFileAsync(filePath, stream);
                }

                test.ProgressText.Text = $"Saved file {filePath}...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Reading file ===========");
                using (var stream = await test.FileAccess.GetFileAsync(filePath))
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    var storedMessage = UTF8Encoding.UTF8.GetString(memoryStream.ToArray());
                    Debug.WriteLine($"Message: {storedMessage}");
                    test.ProgressText.Text = $"Readed file {filePath}...";
                }

                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Existence check ===========");
                Debug.WriteLine($"Directory exists: {await test.FileAccess.ExistsDirectoryAsync(folderPath)}");
                Debug.WriteLine($"File exists: {await test.FileAccess.ExistsFileAsync(filePath)}");

                test.ProgressText.Text = "Checked file and folder existance...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                Debug.WriteLine("=========== Delete folder and files ===========");
                await test.FileAccess.DeleteFileAsync(filePath);
                await test.FileAccess.DeleteDirectoryAsync(rootFolder);

                test.ProgressText.Text = "Deleted test file and folder...";
                await Task.Delay(TimeSpan.FromSeconds(StagesDelaySeconds));

                test.ProgressText.Text = "Finished";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR: {ex}");
                test.ProgressText.Text = "#Error";
            }
            finally
            {
                test.InProgress = false;
            }
        }

        private class FileAccessConfiguration
        {
            public bool InProgress { get; set; }

            public FileAccess FileAccess { get; set; }

            public Text3DMesh ProgressText { get; set; }
        }
    }
}
