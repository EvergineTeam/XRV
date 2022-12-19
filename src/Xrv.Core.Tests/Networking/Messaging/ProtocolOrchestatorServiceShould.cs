using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Networking.Client;
using Evergine.Networking.Connection.Messages;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xrv.Core.Messaging;
using Xrv.Core.Networking;
using Xrv.Core.Networking.Messaging;
using Xunit;

namespace Xrv.Core.Tests.Networking.Messaging
{
    public class ProtocolOrchestatorServiceShould
    {
        private readonly TestEndpoint endpoint1;
        private readonly TestEndpoint endpoint2;

        public ProtocolOrchestatorServiceShould()
        {
            this.endpoint1 = new TestEndpoint();
            this.endpoint2 = new TestEndpoint();
        }

        [Fact]
        public void NotStartAProtocolForMissingRegistration()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);

            // we are registering protocol for endpoint1, but not for endpoint2, so this last
            // may return a protocol deny message
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);

            this.endpoint2.Lifecycle
                .Verify(sender => sender.SendLifecycleMessageToClient(
                    protocolInstance1.Object.CorrelationId,
                    LifecycleMessageType.StartProtocolDenied,
                    this.endpoint1.Peer.Object.Id,
                    It.IsAny<Action<OutgoingMessage>>()),
                    Times.Once);
        }

        [Fact]
        public void SkipProcessingDuplicatedCorrelationId()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint2.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);

            this.endpoint2.Lifecycle
                .Verify(sender => sender.SendLifecycleMessageToClient(
                    protocolInstance1.Object.CorrelationId,
                    LifecycleMessageType.StartProtocolAccepted,
                    this.endpoint1.Peer.Object.Id,
                    It.IsAny<Action<OutgoingMessage>>()),
                    Times.Once);
            this.endpoint2.Lifecycle
                .Verify(sender => sender.SendLifecycleMessageToClient(
                    protocolInstance1.Object.CorrelationId,
                    LifecycleMessageType.StartProtocolDenied,
                    this.endpoint1.Peer.Object.Id,
                    It.IsAny<Action<OutgoingMessage>>()),
                    Times.Once);
        }

        [Fact]
        public void SkipProcessingOfMessagesNotMarkedAsProtocol()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.IsProtocolMessage = false;

            var protocolStart = this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            this.endpoint2.Lifecycle
                .Verify(server => server.SendLifecycleMessageToClient(
                    It.IsAny<Guid>(),
                    It.IsAny<LifecycleMessageType>(),
                    It.IsAny<int>(),
                    It.IsAny<Action<OutgoingMessage>>()));
        }

        [Fact]
        public void InvokeProtocolLifecycleEventsOnStartResponse()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            var protocolStarter = new Mock<ProtocolStarter>(protocolInstance1.Object, this.endpoint1.Lifecycle.Object);
            protocolInstance1
                .Setup(p => p.ProtocolStarter)
                .Returns(protocolStarter.Object);
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            this.endpoint1.SimulateProtocolStartResponse(protocolInstance1.Object.CorrelationId, null);
            protocolStarter
                .Verify(starter => starter.OnProtocolStartResponse(
                    It.Is<StartProtocolResponseMessage>(message => message.CorrelationId == protocolInstance1.Object.CorrelationId)));
        }

        [Fact]
        public async Task RequestForAliveEchoWhenCheckTimeIsReached()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint2.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);

            var orchestator = this.endpoint2.Orchestator;
            // hack internal table to set a old enough date for protocol entry
            orchestator.PeerProtocols.First().LastAliveDate = DateTime.UtcNow.AddHours(-1);

            var tcs = new TaskCompletionSource<bool>();
            orchestator.StillAliveSent += (_, __) =>
            {
                tcs.TrySetResult(true);
            };

            orchestator.Update(orchestator.CheckAliveProtocolsDelay.Add(TimeSpan.FromMilliseconds(20)));
            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMilliseconds(100)));

            this.endpoint2.Lifecycle
                .Verify(sender => sender.SendLifecycleMessageToClient(
                    protocolInstance1.Object.CorrelationId,
                    LifecycleMessageType.AreYouStillAlive,
                    this.endpoint1.Peer.Object.Id,
                    It.IsAny<Action<OutgoingMessage>>()),
                    Times.Once);
        }

        [Fact]
        public void SendBackAnAliveMessageWhenRequested()
        {
            const string ProtocolName = "Protocol1";
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            this.endpoint1.SimulateAreYouStillAliveRequest(protocolInstance1.Object, this.endpoint2);

            this.endpoint1.Lifecycle
                .Verify(sender => sender.SendLifecycleMessageToClient(
                    protocolInstance1.Object.CorrelationId,
                    LifecycleMessageType.ImStillAlive,
                    this.endpoint2.Peer.Object.Id,
                    It.IsAny<Action<OutgoingMessage>>()),
                    Times.Once);
        }

        [Fact]
        public void DelegateInProtocolImplementationForTalkingTypeMessages()
        {
            const string ProtocolName = "Protocol1";
            var messageConverter = new Mock<INetworkingMessageConverter>();
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            protocolInstance1
                .Setup(p => p.InternalCreateMessageInstance(It.IsAny<IIncomingMessage>()))
                .Returns(messageConverter.Object);

            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint2.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            this.endpoint2.SimulateTalking(protocolInstance1.Object, this.endpoint1);

            protocolInstance1
                .Verify(p => p.InternalMessageReceived(
                    It.IsAny<INetworkingMessageConverter>(),
                    this.endpoint1.Peer.Object.Id));
        }

        [Fact]
        public void RemoveProtocolWhenEndMessageIsReceived()
        {
            const string ProtocolName = "Protocol1";
            var messageConverter = new Mock<INetworkingMessageConverter>();
            var protocolInstance1 = new Mock<NetworkingProtocol>(this.endpoint1.NetworkSystem.Object);
            var protocolStarter = new Mock<ProtocolStarter>(protocolInstance1.Object, this.endpoint1.Lifecycle.Object);
            protocolInstance1
                .Setup(p => p.ProtocolStarter)
                .Returns(protocolStarter.Object);

            this.endpoint1.Orchestator.RegisterProtocolInstantiator(ProtocolName, () => protocolInstance1.Object);
            this.endpoint1.SimulateProtocolStart(ProtocolName, protocolInstance1.Object, this.endpoint2);
            Assert.Single(this.endpoint1.Orchestator.SelfProtocols);

            protocolInstance1.Object.InternalEndProtocol();
            Assert.Empty(this.endpoint1.Orchestator.SelfProtocols);
        }

        internal class TestEndpoint
        {
            private readonly Mock<NetworkSystem> networkSystem;
            private readonly ProtocolOrchestatorService orchestator;
            private readonly Mock<ILifecycleMessaging> lifecycle;
            private readonly Mock<IClientServerMessaging> clientServer;
            private readonly Mock<INetworkPeer> peer;
            private static System.Random random = new System.Random();

            public TestEndpoint()
            {
                this.lifecycle = new Mock<ILifecycleMessaging>();
                this.clientServer = new Mock<IClientServerMessaging>();
                this.networkSystem = new Mock<NetworkSystem>(
                    new Mock<XrvService>().Object,
                    new Mock<EntityManager>().Object,
                    new Mock<AssetsService>().Object);

                var clientServer = new Mock<IClientServerMessagingImpl>();
                this.networkSystem.Object.ClientServerMessaging = clientServer.Object;

                this.orchestator = new ProtocolOrchestatorService(this.lifecycle.Object);
                this.peer = new Mock<INetworkPeer>();
                this.peer
                    .Setup(p => p.Id)
                    .Returns(random.Next());
            }

            public Mock<NetworkSystem> NetworkSystem { get => this.networkSystem; }

            public ProtocolOrchestatorService Orchestator { get => this.orchestator; }

            public Mock<ILifecycleMessaging> Lifecycle { get => this.lifecycle; }

            public Mock<IClientServerMessaging> ClientServer { get => this.clientServer; }

            public Mock<INetworkPeer> Peer { get => this.peer; }

            public bool IsProtocolMessage { get; set; } = true;

            public Mock<NetworkingProtocol> RegisterMockedProtocol(string protocolName)
            {
                var protocol = new Mock<NetworkingProtocol>(this.lifecycle.Object);
                this.orchestator.RegisterProtocolInstantiator(protocolName, () => protocol.Object);

                return protocol;
            }

            public Mock<IIncomingMessage> SimulateProtocolStart(string protocolName, NetworkingProtocol protocol, TestEndpoint destination)
            {
                var protocolStart = new Mock<IIncomingMessage>();
                protocolStart
                    .Setup(p => p.IsProtocol)
                    .Returns(true);
                protocolStart
                    .Setup(p => p.CorrelationId)
                    .Returns(protocol.CorrelationId);
                protocolStart
                    .Setup(p => p.To(It.IsAny<StartProtocolRequestMessage>()))
                    .Callback<StartProtocolRequestMessage>(m => m.ProtocolName = protocolName);
                protocolStart
                    .Setup(p => p.Sender)
                    .Returns(this.peer.Object);

                this.orchestator.RegisterSelfProtocol(protocol);
                destination.Orchestator.HandleIncomingMessage(protocolStart.Object, true);
                return protocolStart;
            }

            public void SimulateProtocolStartResponse(Guid correlationId, ProtocolError? error)
            {
                var message = new Mock<IIncomingMessage>();
                message
                    .Setup(p => p.IsProtocol)
                    .Returns(true);
                message
                    .Setup(p => p.CorrelationId)
                    .Returns(correlationId);
                message
                    .Setup(p => p.LifecycleType)
                    .Returns(() =>
                    {
                        return error.HasValue 
                            ? LifecycleMessageType.StartProtocolDenied 
                            : LifecycleMessageType.StartProtocolAccepted;
                    });
                message
                    .Setup(p => p.To(It.IsAny<StartProtocolResponseMessage>()))
                    .Callback<StartProtocolResponseMessage>(m => m.ErrorCode = error);
                message
                    .Setup(p => p.Sender)
                    .Returns(this.peer.Object);

                this.Orchestator.HandleIncomingMessage(message.Object, true);
            }

            public void SimulateAreYouStillAliveRequest(NetworkingProtocol protocol, TestEndpoint destination)
            {
                var message = new Mock<IIncomingMessage>();
                message
                    .Setup(p => p.IsProtocol)
                    .Returns(true);
                message
                    .Setup(p => p.CorrelationId)
                    .Returns(protocol.CorrelationId);
                message
                    .Setup(p => p.LifecycleType)
                    .Returns(LifecycleMessageType.AreYouStillAlive);
                message
                    .Setup(p => p.Sender)
                    .Returns(destination.Peer.Object);

                this.Orchestator.HandleIncomingMessage(message.Object, true);
            }

            public void SimulateTalking(NetworkingProtocol protocol, TestEndpoint destination)
            {
                var message = new Mock<IIncomingMessage>();
                message
                    .Setup(p => p.IsProtocol)
                    .Returns(true);
                message
                    .Setup(p => p.CorrelationId)
                    .Returns(protocol.CorrelationId);
                message
                    .Setup(p => p.LifecycleType)
                    .Returns(LifecycleMessageType.Talking);
                message
                    .Setup(p => p.Sender)
                    .Returns(destination.Peer.Object);

                this.Orchestator.HandleIncomingMessage(message.Object, true);
            }
        }
    }
}
