using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Threading;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Platform;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrvSamples.Scenes
{
    internal class StorageScene : BaseScene
    {
        private PressableButton localApplicationDataButton;
        private Text3DMesh localApplicationDataText;
        private bool localApplicationInProgress;

        public override void Initialize()
        {
            base.Initialize();

            var entityManager = this.Managers.EntityManager;
            this.localApplicationDataButton = entityManager.FindAllByTag("localAppData").First().FindComponentInChildren<PressableButton>();
            this.localApplicationDataText = entityManager.FindAllByTag("localAppData").First().FindComponentInChildren<Text3DMesh>();
            this.localApplicationDataButton.ButtonReleased += this.LocalApplicationDataButton_ButtonReleased;

            // Uncomment to test this in Android (not Quest)
            // EvergineForegroundTask.Run(() => this.LocalApplicationDataButton_ButtonReleased(null, EventArgs.Empty));
        }

        private async void LocalApplicationDataButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.localApplicationInProgress)
            {
                return;
            }

            this.localApplicationInProgress = true;

            var fileAccess = new Xrv.Core.Storage.ApplicationDataFileAccess();

            this.localApplicationDataText.Text = $"Starting local data access test...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Enumerating directories ===========");
            var directories = await fileAccess.EnumerateDirectoriesAsync();
            foreach (var directory in directories)
            {
                Debug.WriteLine(directory);
            }

            this.localApplicationDataText.Text = $"There are {directories.Count()} directories in root folder...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Enumerating files ===========");
            var files = await fileAccess.EnumerateFilesAsync();
            foreach (var file in files)
            {
                Debug.WriteLine(file);
            }

            this.localApplicationDataText.Text = $"There are {files.Count()} files in root folder...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Creating directory and file ===========");
            const string fileName = "myfile.bin";
            var folderPath = Path.Combine("test", "myfolder");
            var filePath = Path.Combine(folderPath, fileName);
            await fileAccess.CreateDirectoryAsync(folderPath);

            this.localApplicationDataText.Text = $"Created folder {folderPath}...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            var secretMessage = "This is a secret message!";
            byte[] messageData = UTF8Encoding.UTF8.GetBytes(secretMessage);
            using (var stream = new MemoryStream(messageData))
            {
                await fileAccess.WriteFileAsync(filePath, stream);
            }

            this.localApplicationDataText.Text = $"Saved file {filePath}...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Reading file ===========");
            using (var stream = await fileAccess.GetFileAsync(filePath))
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                var storedMessage = UTF8Encoding.UTF8.GetString(memoryStream.ToArray());
                Debug.WriteLine($"Message: {storedMessage}");
                this.localApplicationDataText.Text = $"Readed file {filePath}...";
            }

            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Existence check ===========");
            Debug.WriteLine($"Directory exists: {await fileAccess.ExistsDirectoryAsync(folderPath)}");
            Debug.WriteLine($"File exists: {await fileAccess.ExistsFileAsync(filePath)}");

            this.localApplicationDataText.Text = "Checked file and folder existance...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            Debug.WriteLine("=========== Delete folder and files ===========");
            await fileAccess.DeleteFileAsync(filePath);
            await fileAccess.DeleteDirectoryAsync(Path.Combine(folderPath, ".."));

            this.localApplicationDataText.Text = "Deleted test file and folder...";
            await Task.Delay(TimeSpan.FromSeconds(3));

            this.localApplicationDataText.Text = "Finished";
            this.localApplicationInProgress = false;
        }
    }
}
