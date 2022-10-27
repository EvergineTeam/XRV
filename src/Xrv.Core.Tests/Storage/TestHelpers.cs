using System.IO;
using System.Text;
using System.Threading.Tasks;
using FileAccess = Xrv.Core.Storage.FileAccess;

namespace Xrv.Core.Tests.Storage
{
    internal static class TestHelpers
    {
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

        public static async Task PrepareTestFileSystem(
            FileAccess fileAccess, 
            int numberOfDirectories, 
            int numberOfFilesPerDirectory)
        {
            string filePath;

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

            filePath = "file.txt";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(filePath)))
            {
                await fileAccess.WriteFileAsync(filePath, stream);
            }
        }
    }
}
