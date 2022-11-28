namespace Xrv.Core.Tests.Storage
{
    internal class StorageConfiguration
    {
        public AzureStorageConfiguration AzureStorage { get; set; } = new AzureStorageConfiguration();

        public class AzureStorageConfiguration
        {
            public string AccountConnectionString { get; set; }

            public BlobsConfiguration Blobs { get; set; } = new BlobsConfiguration();

            public FileShareConfiguration FileShare { get; set; } = new FileShareConfiguration();

            public class BlobsConfiguration
            {
                public string ContainerName { get; set; }

                public string UriWithSas { get; set; }

                public string UriWithoutSas { get; set; }

                public string Sas { get; set; }
            }

            public class FileShareConfiguration
            {
                public string ShareName { get; set; }

                public string UriWithSas { get; set; }

                public string UriWithoutSas { get; set; }

                public string Sas { get; set; }
            }
        }
    }
}
