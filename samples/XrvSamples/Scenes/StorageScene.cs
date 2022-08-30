using Evergine.Framework;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace XrvSamples.Scenes
{
    internal class StorageScene : Scene
    {
        public override void Initialize()
        {
            base.Initialize();

            Task.Run(async () =>
            {
                var fileAccess = new Xrv.Core.Storage.ApplicationDataFileAccess();

                Debug.WriteLine("=========== Enumerating directories ===========");
                var directories = await fileAccess.EnumerateDirectoriesAsync();
                foreach (var directory in directories)
                {
                    Debug.WriteLine(directory);
                }

                Debug.WriteLine("=========== Enumerating files ===========");
                var files = await fileAccess.EnumerateFilesAsync();
                foreach (var file in files)
                {
                    Debug.WriteLine(file);
                }

                Debug.WriteLine("=========== Creating directory and file ===========");
                var folderPath = Path.Combine("test", "myfolder");
                var filePath = Path.Combine(folderPath, "myfile.bin");
                await fileAccess.CreateDirectoryAsync(folderPath);

                var secretMessage = "This is a secret message!";
                byte[] messageData = UTF8Encoding.UTF8.GetBytes(secretMessage);
                using (var stream = new MemoryStream(messageData))
                {
                    await fileAccess.WriteFileAsync(filePath, stream);
                }

                Debug.WriteLine("=========== Reading file ===========");
                using (var stream = await fileAccess.GetFileAsync(filePath))
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    var storedMessage = UTF8Encoding.UTF8.GetString(memoryStream.ToArray());
                    Debug.WriteLine($"Message: {storedMessage}");
                }

                Debug.WriteLine("=========== Existence check ===========");
                Debug.WriteLine($"Directory exists: {await fileAccess.ExistsDirectoryAsync(folderPath)}");
                Debug.WriteLine($"File exists: {await fileAccess.ExistsFileAsync(filePath)}");

                Debug.WriteLine("=========== Delete folder and files ===========");
                await fileAccess.DeleteFileAsync(filePath);
                await fileAccess.DeleteDirectoryAsync(Path.Combine(folderPath, ".."));
            })
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception;
                }
            });
        }
    }
}
