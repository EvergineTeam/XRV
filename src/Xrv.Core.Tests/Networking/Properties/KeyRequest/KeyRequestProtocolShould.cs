using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Networking;
using Evergine.Networking.Client;
using Evergine.Networking.Components;
using Moq;
using System;
using System.Threading.Tasks;
using Xrv.Core.Messaging;
using Xrv.Core.Networking;
using Xrv.Core.Networking.Messaging;
using Xrv.Core.Networking.Properties.KeyRequest;
using Xrv.Core.Networking.Properties.KeyRequest.Messages;
using Xunit;

namespace Xrv.Core.Tests.Networking.Properties.KeyRequest
{
    public class KeyRequestProtocolShould
    {
        private readonly KeyRequestProtocol protocol;
        private readonly Mock<NetworkSystem> networking;
        private readonly Mock<IKeyStore> keyStore;
        private readonly Mock<IClientServerMessagingImpl> clientServerImpl;
        private readonly Mock<ProtocolStarter> protocolStarter;
        private readonly TestSession session;

        public KeyRequestProtocolShould()
        {
            this.networking = new Mock<NetworkSystem>(
                new Mock<XrvService>().Object,
                new Mock<EntityManager>().Object,
                new Mock<AssetsService>().Object);
            this.keyStore = new Mock<IKeyStore>();
            this.clientServerImpl = new Mock<IClientServerMessagingImpl>();
            this.session = new TestSession
            {
                IsHost = true,
            };

            this.networking.Object.ClientServerMessaging = this.clientServerImpl.Object;
            this.networking.Object.Session = this.session;

            this.protocol = new KeyRequestProtocol(this.networking.Object, this.keyStore.Object);
            this.protocolStarter = new Mock<ProtocolStarter>(this.protocol, this.clientServerImpl.Object);
            this.protocol.ProtocolStarter = this.protocolStarter.Object;
        }

        [Fact]
        public async Task ElevateProtocolStartExceptions()
        {
            this.protocolStarter
                .Setup(starter => starter.StartAsync())
                .ThrowsAsync(new InvalidOperationException());
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => this.protocol.RequestSingleKeyAsync(NetworkPropertyProviderFilter.Room));
        }

        [Fact]
        public void IgnoreServerMessagesIfThisIsNotAServer()
        {
            var request = new RequestNumberOfKeysMessage
            {
                NumberOfKeys = 1,
                ProviderType = NetworkPropertyProviderFilter.Room,
                Type = RequestKeyMessageType.ClientRequestKeys,
            };
            this.session.IsHost = false;
            this.protocol.InternalMessageReceived(request, 1);

            this.clientServerImpl
                .Verify(
                    server => server.SendProtocolMessageToClient(
                        It.IsAny<NetworkingProtocol>(),
                        It.IsAny<INetworkingMessageConverter>(),
                        It.IsAny<int>()),
                    Times.Never());
        }

        [Fact]
        public async Task RequestExactlyTheNumberOfRequiredKeys()
        {
            const int NumberOfKeys = 4;

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                var message = new AssignedKeysMessage();
                this.protocol.InternalMessageReceived(message, 1);

                var confirmation = new RequestKeyProtocolMessage
                {
                    Type = RequestKeyMessageType.ServerConfirmsKeysConfirmation,
                };
                this.protocol.InternalMessageReceived(confirmation, 1);
            });

            await this.protocol.RequestSetOfKeysAsync(NumberOfKeys, NetworkPropertyProviderFilter.Room);

            Func<INetworkingMessageConverter, bool> matchingFunc = new Func<INetworkingMessageConverter, bool>(m =>
            {
                if (m is RequestNumberOfKeysMessage numberOfKeys)
                {
                    return numberOfKeys.NumberOfKeys == NumberOfKeys;
                }

                return false;
            });

            this.clientServerImpl
                .Verify(
                    server => server.SendProtocolMessageToServer(
                        this.protocol,
                        It.Is<INetworkingMessageConverter>(m => matchingFunc(m))),
                    Times.Once());
        }

        [Fact]
        public async Task ThrowExceptionIfServerRejectsKeyRequest()
        {
            var message = new RequestKeyProtocolMessage
            {
                Type = RequestKeyMessageType.ServerRejectsKeysRequest
            };

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                this.protocol.InternalMessageReceived(message, 1);
            });

            Exception exception = null;
            try
            {
                await this.protocol.RequestSetOfKeysAsync(2, NetworkPropertyProviderFilter.Room);
            }
            catch (AggregateException ae)
            {
                exception = ae.InnerException;
            }

            Assert.Equal(typeof(FullKeyStoreException), exception?.GetType());
        }

        [Fact]
        public void ReserveRequestedKeys()
        {
            const int NumberOfKeys = 4;
            var message = new RequestNumberOfKeysMessage
            {
                NumberOfKeys = NumberOfKeys,
                ProviderType = NetworkPropertyProviderFilter.Room,
            };

            this.protocol.InternalMessageReceived(message, 1);
            this.keyStore
                .Verify(store => store.ReserveKeys(
                    NumberOfKeys, 
                    this.protocol.CorrelationId, 
                    It.IsAny<int>(),
                    message.ProviderType));
        }

        [Fact]
        public async Task ReceiveAssignedKeysFromServer()
        {
            var message = new AssignedKeysMessage
            {
                Keys = new byte[] { 0x01, 0x04 },
            };

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                this.protocol.InternalMessageReceived(message, 1);

                var confirmation = new RequestKeyProtocolMessage
                {
                    Type = RequestKeyMessageType.ServerConfirmsKeysConfirmation,
                };
                this.protocol.InternalMessageReceived(confirmation, 1);
            });

            var keys = await this.protocol.RequestSetOfKeysAsync((byte)message.Keys.Length, NetworkPropertyProviderFilter.Room);
            Assert.Equal(message.Keys, keys);
        }

        [Fact]
        public void SendsConfirmationForKeysSentFromServer()
        {
            var message = new AssignedKeysMessage
            {
                Keys = new byte[] { 0x01, 0x04 },
            };
            this.protocol.InternalMessageReceived(message, 1);
            this.clientServerImpl
                .Verify(
                    server => server.SendProtocolMessageToServer(
                        this.protocol, 
                        It.Is<INetworkingMessageConverter>(m => ((RequestKeyProtocolMessage)m).Type == RequestKeyMessageType.ClientConfirmsKeysReservation)),
                    Times.Once());
        }


        [Fact]
        public async Task ThrowExceptionIfServerRejectsKeysConfirmation()
        {
            var message = new RequestKeyProtocolMessage
            {
                Type = RequestKeyMessageType.ServerRejectsKeysConfirmation,
            };

            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                this.protocol.InternalMessageReceived(message, 1);
            });

            Exception exception = null;
            try
            {
                await this.protocol.RequestSetOfKeysAsync(2, NetworkPropertyProviderFilter.Room);
            }
            catch (AggregateException ae)
            {
                exception = ae.InnerException;
            }

            Assert.Equal(typeof(KeysReservationTimeExpiredException), exception?.GetType());
        }

        [Fact]
        public void ConfirmKeysReservationToClient()
        {
            var message = new RequestKeyProtocolMessage
            {
                Type = RequestKeyMessageType.ClientConfirmsKeysReservation,
            };
            this.protocol.InternalMessageReceived(message, 1);
            this.keyStore
                .Verify(store => store.ConfirmKeys(this.protocol.CorrelationId, It.IsAny<int>()));
        }

        [Fact]
        public void ClientCancelsKeyReservation()
        {
            var message = new RequestKeyProtocolMessage
            {
                Type = RequestKeyMessageType.ClientCancelsKeysReservation,
            };
            this.protocol.InternalMessageReceived(message, 1);
            this.keyStore
                .Verify(store => store.FreeKeys(this.protocol.CorrelationId, It.IsAny<int>()));
        }

        private class TestSession : SessionInfo
        {
            internal TestSession()
                : base(null, null)
            {
            }

            public override bool CurrentUserIsHost => this.IsHost;

            public bool IsHost { get; set; }
        }
    }
}
