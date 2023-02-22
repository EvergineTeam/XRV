using Evergine.Xrv.Core.Storage;
using System;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Storage
{
    public class AzureBlobFileAccessInstantiationShould
    {
        [Fact]
        public void SupportPublicContainerUris()
        {
           AzureBlobFileAccess.CreateFromUri(new Uri("https://dummyaccount.blob.core.windows.net/container"));
        }
    }
}
