// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Xrv.Core.Networking.Messaging
{
    internal class ProtocolOrchestatorService : UpdatableService
    {
        private readonly ILifecycleMessaging lifecycle;
        private readonly ConcurrentDictionary<Guid, NetworkingProtocol> selfProtocols;
        private readonly ConcurrentDictionary<Guid, ClientServerProtocolEntry> peerProtocols;
        private readonly Dictionary<string, Func<NetworkingProtocol>> protocolInstantiators;

        private readonly List<ClientServerProtocolEntry> entriesToCheckAlive;
        private ClientServerProtocolEntry entryBeingUpdated;
        private TimeSpan currentCheckAliveProtocolsDelay;

        internal ProtocolOrchestatorService(ILifecycleMessaging lifecycle)
        {
            this.lifecycle = lifecycle;
            this.selfProtocols = new ConcurrentDictionary<Guid, NetworkingProtocol>();
            this.peerProtocols = new ConcurrentDictionary<Guid, ClientServerProtocolEntry>();
            this.protocolInstantiators = new Dictionary<string, Func<NetworkingProtocol>>();
            this.entriesToCheckAlive = new List<ClientServerProtocolEntry>();
        }

        public event EventHandler StillAliveSent;

        public TimeSpan CheckAliveProtocolsDelay { get; set; } = TimeSpan.FromSeconds(5);

        public IReadOnlyCollection<NetworkingProtocol> SelfProtocols { get => this.selfProtocols.Values.ToList().AsReadOnly(); }

        public IReadOnlyCollection<ClientServerProtocolEntry> PeerProtocols { get => this.peerProtocols.Values.ToList().AsReadOnly(); }

        public void RegisterProtocolInstantiator<TInstantiator>(string protocolName, Func<TInstantiator> instantiatorFunc)
            where TInstantiator : NetworkingProtocol
        {
            this.protocolInstantiators[protocolName] = instantiatorFunc;
        }

        public void RegisterSelfProtocol(NetworkingProtocol protocol)
        {
            if (this.selfProtocols.TryAdd(protocol.CorrelationId, protocol))
            {
                protocol.Ended += this.Protocol_Ended;
            }
        }

        public void UnregisterSelfProtocol(NetworkingProtocol protocol)
        {
            protocol.Ended -= this.Protocol_Ended;
            this.selfProtocols.TryRemove(protocol.CorrelationId, out var _);
        }

        public override void Update(TimeSpan gameTime)
        {
            this.currentCheckAliveProtocolsDelay = this.currentCheckAliveProtocolsDelay.Add(gameTime);
            if (this.currentCheckAliveProtocolsDelay < this.CheckAliveProtocolsDelay)
            {
                return;
            }

            this.currentCheckAliveProtocolsDelay = TimeSpan.Zero;
            _ = Task.Run(() =>
            {
                // Remove previously entries detected as required to be check as alive
                // that are still with a too old alive date
                var lastValidAliveDate = DateTime.UtcNow - this.CheckAliveProtocolsDelay;

                for (int i = 0; i < this.entriesToCheckAlive.Count; i++)
                {
                    this.entryBeingUpdated = this.entriesToCheckAlive[i];
                    System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Update] Checking candidate to be removed: {this.entryBeingUpdated.CorrelationId}");

                    if (this.entryBeingUpdated.LastAliveDate < lastValidAliveDate)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Update] Removing: {this.entryBeingUpdated.CorrelationId}");
                        this.peerProtocols.TryRemove(this.entryBeingUpdated.CorrelationId, out _);
                    }
                }

                this.entriesToCheckAlive.Clear();

                // Update entries to be checked on next iteration
                this.entriesToCheckAlive.AddRange(
                    this.peerProtocols
                        .Where(entry => entry.Value.LastAliveDate < lastValidAliveDate)
                        .Select(entry => entry.Value));

                for (int i = 0; i < this.entriesToCheckAlive.Count; i++)
                {
                    this.entryBeingUpdated = this.entriesToCheckAlive[i];
                    System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Update] Requesting alive echo for {this.entryBeingUpdated.CorrelationId}");
                    this.lifecycle.SendLifecycleMessageToClient(
                        this.entryBeingUpdated.CorrelationId,
                        LifecycleMessageType.AreYouStillAlive,
                        false,
                        this.entryBeingUpdated.Sender.Id);
                }

                if (this.entriesToCheckAlive.Any())
                {
                    this.StillAliveSent?.Invoke(this, EventArgs.Empty);
                }
            })
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Update] Error checking alive entries: {t.Exception}");
                }
            });
        }

        internal void HandleIncomingMessage(IIncomingMessage message, bool receivedAsServer)
        {
            if (!message.IsProtocol)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Not a protocol message, skip network message");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received message {message.LifecycleType} with correlation {message.CorrelationId}");

            switch (message.LifecycleType)
            {
                case LifecycleMessageType.StartProtocol:
                    this.HandleProtocolStart(message.CorrelationId, message, receivedAsServer);
                    break;
                case LifecycleMessageType.StartProtocolDenied:
                case LifecycleMessageType.StartProtocolAccepted:
                    this.HandleProtocolStartResponse(message.CorrelationId, message);
                    break;
                case LifecycleMessageType.AreYouStillAlive:
                    this.HandleAreYouStillAlive(message.CorrelationId, message, receivedAsServer);
                    break;
                case LifecycleMessageType.ImStillAlive:
                    this.HandleImStillAlive(message.CorrelationId, message);
                    break;
                case LifecycleMessageType.Talking:
                    this.HandleTalking(message.CorrelationId, message, receivedAsServer);
                    break;
                case LifecycleMessageType.EndProtocol:
                    this.HandleProtocolEnd(message.CorrelationId, message);
                    break;
            }
        }

        private void HandleProtocolStart(Guid correlationId, IIncomingMessage message, bool receivedAsServer)
        {
            var request = new StartProtocolRequestMessage();
            message.To(request);

            // Check that protocol has not already been started
            if (this.peerProtocols.ContainsKey(correlationId))
            {
                var deniedMessage = new StartProtocolResponseMessage
                {
                    ErrorCode = ProtocolError.DuplicatedCorrelationId,
                };
                this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolDenied, receivedAsServer, message.Sender.Id, deniedMessage.WriteTo);
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received protocol start message for an already started protocol: {correlationId}. {LifecycleMessageType.StartProtocolDenied} signal sent.");

                return;
            }

            // Instantiate protocol
            if (!this.protocolInstantiators.ContainsKey(request.ProtocolName))
            {
                var deniedMessage = new StartProtocolResponseMessage
                {
                    ErrorCode = ProtocolError.MissingProtocolInstantiator,
                };
                this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolDenied, receivedAsServer, message.Sender.Id, deniedMessage.WriteTo);
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Protocol instantiator not found for class name: {request.ProtocolName}. {LifecycleMessageType.StartProtocolDenied} signal sent.");

                return;
            }

            NetworkingProtocol protocolInstance;
            if (this.selfProtocols.ContainsKey(correlationId))
            {
                protocolInstance = this.selfProtocols[correlationId];
            }
            else
            {
                protocolInstance = this.protocolInstantiators[request.ProtocolName].Invoke();
                protocolInstance.CorrelationId = correlationId;
                protocolInstance.ProtocolStarter = new ProtocolStarter(protocolInstance, this.lifecycle)
                {
                    ActAsServer = receivedAsServer,
                    TargetClientId = message.Sender.Id,
                };
                protocolInstance.Ended += this.Protocol_Ended;
            }

            // Annotate protocol and sender
            var entry = new ClientServerProtocolEntry
            {
                CorrelationId = correlationId,
                LastAliveDate = DateTime.UtcNow,
                Protocol = protocolInstance,
                Sender = message.Sender,
            };

            this.peerProtocols[correlationId] = entry;
            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Protocol {protocolInstance.Name} ({correlationId}) added to internal table.");

            var successMessage = new StartProtocolResponseMessage();
            this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolAccepted, receivedAsServer, entry.Sender.Id, successMessage.WriteTo);
            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] {LifecycleMessageType.StartProtocolAccepted} signal sent to {entry.Sender.Id} for {protocolInstance.Name} ({correlationId})");
        }

        private void HandleProtocolStartResponse(Guid correlationId, IIncomingMessage message)
        {
            NetworkingProtocol protocol;
            if (!this.selfProtocols.TryGetValue(correlationId, out protocol))
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received protocol start response message, but not found correlation: {correlationId}. Skip message.");
                return;
            }

            var response = new StartProtocolResponseMessage
            {
                CorrelationId = correlationId,
            };
            message.To(response);

            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Delegating response message {protocol.Name} ({correlationId}) to {nameof(ProtocolStarter)}");
            protocol.ProtocolStarter.OnProtocolStartResponse(response);
        }

        private void HandleAreYouStillAlive(Guid correlationId, IIncomingMessage message, bool receivedAsServer)
        {
            // Check that sender is related with correlationId
            NetworkingProtocol protocol;
            if (!this.selfProtocols.TryGetValue(correlationId, out protocol))
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.AreYouStillAlive} message, but not found correlation: {correlationId}");
                return;
            }

            // Send back ImStillAlive command
            this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.ImStillAlive, receivedAsServer, message.Sender.Id);
            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] {LifecycleMessageType.ImStillAlive} signal sent to {message.Sender.Id} for {protocol.Name} ({correlationId})");
        }

        private void HandleImStillAlive(Guid correlationId, IIncomingMessage message)
        {
            // Check that sender is related with correlationId
            ClientServerProtocolEntry entry;
            if (!this.peerProtocols.TryGetValue(correlationId, out entry))
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.ImStillAlive} message, but not found correlation: {correlationId}");
                return;
            }

            if (entry.Sender.Id != message.Sender.Id)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.ImStillAlive} message, but not from expected sender: {message.Sender.Id} ({entry.Sender.Id} expected)");
                return;
            }

            // Update last alive date
            entry.LastAliveDate = DateTime.UtcNow;
        }

        private void HandleTalking(Guid correlationId, IIncomingMessage message, bool receivedAsServer)
        {
            // Check that sender is related with correlationId
            ClientServerProtocolEntry entry;
            if (!this.peerProtocols.TryGetValue(correlationId, out entry))
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.Talking} message, but not found correlation: {correlationId}");
                return;
            }

            if (entry.Sender.Id != message.Sender.Id)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.Talking} message, but not from expected sender: {message.Sender.Id} ({entry.Sender.Id} expected)");
                return;
            }

            // Update last alive date
            entry.LastAliveDate = DateTime.UtcNow;

            // Delegate in protocol
            INetworkingMessageConverter protocolMessageInstance = entry.Protocol.InternalCreateMessageInstance(message);
            System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Delegating in protocol logic {entry.Protocol.Name} ({correlationId})");

            if (receivedAsServer)
            {
                entry.Protocol.InternalMessageReceivedAsServer(protocolMessageInstance, message.Sender.Id);
            }
            else
            {
                entry.Protocol.InternalMessageReceivedAsClient(protocolMessageInstance, message.Sender.Id);
            }
        }

        private void HandleProtocolEnd(Guid correlationId, IIncomingMessage message)
        {
            bool isAnyPeerProtocol = this.peerProtocols.ContainsKey(correlationId);
            bool isAnySelfProtocol = this.selfProtocols.ContainsKey(correlationId);

            if (!isAnyPeerProtocol && !isAnySelfProtocol)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(ProtocolOrchestatorService)}][Messages] Received {LifecycleMessageType.EndProtocol} message for an unknown protocol: {correlationId}");
                return;
            }

            // TODO: check sender
            this.selfProtocols.TryRemove(correlationId, out var _);
            this.peerProtocols.TryRemove(correlationId, out var _);
        }

        private void Protocol_Ended(object sender, EventArgs e)
        {
            if (sender is NetworkingProtocol protocol)
            {
                protocol.Ended -= this.Protocol_Ended;
                if (this.selfProtocols.ContainsKey(protocol.CorrelationId))
                {
                    this.UnregisterSelfProtocol(protocol);
                }

                if (this.peerProtocols.ContainsKey(protocol.CorrelationId))
                {
                    this.peerProtocols.TryRemove(protocol.CorrelationId, out var _);
                }
            }
        }

        internal class ClientServerProtocolEntry
        {
            public Guid CorrelationId { get; set; }

            public NetworkingProtocol Protocol { get; set; }

            public DateTime LastAliveDate { get; set; }

            public INetworkPeer Sender { get; set; }
        }
    }
}
