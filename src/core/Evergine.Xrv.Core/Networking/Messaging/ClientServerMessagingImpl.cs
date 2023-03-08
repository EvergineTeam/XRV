// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.Services;
using Evergine.Networking.Client;
using Evergine.Networking.Connection;
using Evergine.Networking.Connection.Messages;
using Evergine.Networking.Messages;
using Evergine.Networking.Server;
using System;
using System.Linq;
using Evergine.Xrv.Core.Networking.Extensions;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    internal class ClientServerMessagingImpl : Service, IClientServerMessagingImpl
    {
        private readonly MatchmakingServerService server;
        private readonly MatchmakingClientService client;

        public ClientServerMessagingImpl(MatchmakingServerService server, MatchmakingClientService client)
        {
            this.server = server;
            this.client = client;

            this.server.MessageReceivedFromClient += this.Server_MessageReceivedFromClient;
            this.client.MessageReceivedFromPlayer += this.Client_MessageReceivedFromPlayer;
        }

        public bool IsConnected { get => this.client.IsConnectedAndReady; }

        public ProtocolOrchestatorService Orchestator { get; set; } // TODO review

        public Action<IIncomingMessage, bool> IncomingMessageCallback { get; set; }

        public void RegisterSelfProtocol(NetworkingProtocol protocol) => this.Orchestator.RegisterSelfProtocol(protocol);

        public void UnregisterSelfProtocol(NetworkingProtocol protocol) => this.Orchestator.UnregisterSelfProtocol(protocol);

        public void SendLifecycleMessageToClient(Guid correlationId, LifecycleMessageType type, int targetClientId, Action<OutgoingMessage> beforeSending = null)
        {
            bool iAmMasterClient = this.client.LocalPlayer.IsMasterClient;
            OutgoingMessage message = iAmMasterClient ? this.server.CreateMessage() : this.client.CreateMessage();
            this.InitializeOutgoingMessage(message, correlationId, type);
            beforeSending?.Invoke(message);

            if (targetClientId == 0)
            {
                this.client.SendToServer(message, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                var receiver = this.client.CurrentRoom?.RemotePlayers?.FirstOrDefault(player => player.Id == targetClientId);
                this.client.SendToPlayer(message, receiver, DeliveryMethod.ReliableOrdered);
            }
        }

        public void SendLifecycleMessageToServer(Guid correlationId, LifecycleMessageType type, Action<OutgoingMessage> beforeSending = null)
        {
            OutgoingMessage message = this.client.CreateMessage();
            this.InitializeOutgoingMessage(message, correlationId, type);
            beforeSending?.Invoke(message);
            this.client.SendToServer(message, DeliveryMethod.ReliableOrdered);
        }

        public void SendProtocolMessageToClient(NetworkingProtocol protocol, INetworkingMessageConverter message, int clientId)
        {
            this.SendLifecycleMessageToClient(protocol.CorrelationId, LifecycleMessageType.Talking, clientId, message.WriteTo);
        }

        public void SendProtocolMessageToServer(NetworkingProtocol protocol, INetworkingMessageConverter message)
        {
            this.SendLifecycleMessageToServer(protocol.CorrelationId, LifecycleMessageType.Talking, message.WriteTo);
        }

        private void InitializeOutgoingMessage(OutgoingMessage message, Guid correlationId, LifecycleMessageType type)
        {
            message.Write(true);
            message.Write((byte)type);
            message.Write(correlationId);
        }

        private void Server_MessageReceivedFromClient(object sender, MessageReceivedEventArgs args)
        {
            var senderClient = this.server.FindPlayer(args.FromEndpoint);
            var message = new IncomingMessageWrapper(args.ReceivedMessage)
            {
                Sender = new NetworkPeerWrapper
                {
                    Peer = senderClient,
                },
            };
            this.IncomingMessageCallback.Invoke(message, true);
        }

        private void Client_MessageReceivedFromPlayer(object sender, MessageFromPlayerEventArgs args)
        {
            var message = new IncomingMessageWrapper(args.ReceivedMessage)
            {
                Sender = new NetworkPeerWrapper
                {
                    Peer = args.FromPlayer,
                },
            };
            this.IncomingMessageCallback.Invoke(message, false);
        }
    }
}
