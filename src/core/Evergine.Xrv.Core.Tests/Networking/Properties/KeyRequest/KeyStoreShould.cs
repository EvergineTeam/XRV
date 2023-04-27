using Evergine.Networking.Components;
using System;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Networking.Properties.KeyRequest
{
    public class KeyStoreShould
    {
        private readonly KeyStore keyStore;

        public KeyStoreShould()
        {
            this.keyStore = new KeyStore();
        }

        [Fact]
        public void ReserveKeys()
        {
            const int NumberOfKeys = 5;
            var correlationId = Guid.NewGuid();
            var ownerId = 213123;
            var filter = NetworkPropertyProviderFilter.Room;

            var keys = this.keyStore.ReserveKeys(NumberOfKeys, correlationId, ownerId, filter);
            var internalDict = this.keyStore.GetDictionaryByCorrelationId(correlationId);
            Assert.Equal(NumberOfKeys, keys.Length);
            Assert.Equal(NumberOfKeys, internalDict.Values.Count(v => v.CorrelationId == correlationId));
        }

        [Fact]
        public void ReserveKeysForCore()
        {
            var coreKeys = new byte[] { 0x00, 0x01, 0x02 };
            var correlationId = Guid.Empty;
            this.keyStore.ReserveKeysForCore(coreKeys, NetworkPropertyProviderFilter.Room);

            var internalDict = this.keyStore.GetDictionaryByCorrelationId(correlationId);
            Assert.Equal(coreKeys.Length, internalDict.Count());
            Assert.Equal(coreKeys.Length, internalDict.Values.Count(v => v.CorrelationId == correlationId));
        }

        [Fact]
        public void SaveMetadataForReservedKeys()
        {
            var correlationId = Guid.NewGuid();
            var ownerId = 213123;
            var keys = this.keyStore.ReserveKeys(3, correlationId, ownerId, NetworkPropertyProviderFilter.Room);
            foreach (var key in keys)
            {
                Assert.Equal(correlationId, key.CorrelationId);
                Assert.Equal(ownerId, key.ReservedByClientId);
            }
        }

        [Fact]
        public void ThrowExceptionIfThereIsNoSpaceForMoreKeys()
        {
            var keys = this.keyStore.ReserveKeys(200, Guid.NewGuid(), 1, NetworkPropertyProviderFilter.Room);
            Assert.Throws<FullKeyStoreException>(() => this.keyStore.ReserveKeys(200, Guid.NewGuid(), 1, NetworkPropertyProviderFilter.Room));
        }

        [Fact]
        public async Task RemoveKeysTharAreNotConfirmedInTime()
        {
            this.keyStore.KeyReservationTime = TimeSpan.FromMilliseconds(20);
            this.keyStore.ReserveKeys(2, Guid.NewGuid(), 1, NetworkPropertyProviderFilter.Room);

            var waitFor = this.keyStore.KeyReservationTime.Add(TimeSpan.FromMilliseconds(5));
            await Task.Delay(waitFor);
            this.keyStore.Update(waitFor);
            this.keyStore.Flush();

            var internalDict = this.keyStore.GetDictionaryByFilter(NetworkPropertyProviderFilter.Room);
            Assert.Empty(internalDict);
        }

        [Fact]
        public async Task ConfirmKeysInTime()
        {
            const int NumberOfKeys = 2;
            var correlationId = Guid.NewGuid();
            var ownerId = 123123;
            this.keyStore.ReserveKeys(NumberOfKeys, correlationId, ownerId, NetworkPropertyProviderFilter.Room);

            var waitFor = TimeSpan.FromMilliseconds(5);
            await Task.Delay(waitFor);
            this.keyStore.ConfirmKeys(correlationId, ownerId);
            this.keyStore.Update(waitFor);
            this.keyStore.Flush();

            var internalDict = this.keyStore.GetDictionaryByFilter(NetworkPropertyProviderFilter.Room);
            Assert.Equal(NumberOfKeys, internalDict.Count);
            Assert.True(internalDict.Values.All(v => v.IsConfirmed));
        }

        [Fact]
        public async Task ThrowsExceptionWhenTryingToConfirmKeysNotInTime()
        {
            const int NumberOfKeys = 2;
            var correlationId = Guid.NewGuid();
            var ownerId = 123123;
            this.keyStore.KeyReservationTime = TimeSpan.FromMilliseconds(20);
            this.keyStore.ReserveKeys(NumberOfKeys, correlationId, ownerId, NetworkPropertyProviderFilter.Room);

            var waitFor = this.keyStore.KeyReservationTime * 2;
            await Task.Delay(waitFor);
            Assert.Throws<KeysToConfirmNotAvailableException>(() => this.keyStore.ConfirmKeys(correlationId, ownerId));
        }

        [Fact]
        public void RemoveKeysForCorrelation()
        {
            var correlationId = Guid.NewGuid();
            this.keyStore.ReserveKeys(2, correlationId, 1, NetworkPropertyProviderFilter.Room);
            this.keyStore.FreeKeys(correlationId, 1);

            var internalDict = this.keyStore.GetDictionaryByFilter(NetworkPropertyProviderFilter.Room);
            Assert.Empty(internalDict);
        }
    }
}
