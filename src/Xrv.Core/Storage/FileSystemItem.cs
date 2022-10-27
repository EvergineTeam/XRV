using System;

namespace Xrv.Core.Storage
{
    public abstract class FileSystemItem
    {
        private DateTime? creationTime;
        private DateTime? modificationTime;

        protected FileSystemItem(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }

        public DateTime? CreationTime
        {
            get => this.creationTime?.ToLocalTime();

            set => this.creationTime = value?.ToUniversalTime();
        }

        public DateTime? ModificationTime
        {
            get => this.modificationTime?.ToLocalTime();

            set => this.modificationTime = value?.ToUniversalTime();
        }
    }
}
