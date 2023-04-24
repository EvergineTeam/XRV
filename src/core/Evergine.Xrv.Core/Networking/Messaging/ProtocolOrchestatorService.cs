// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    internal class ProtocolOrchestatorService : UpdatableService
    {
        private readonly ILifecycleMessaging lifecycle;
        private readonly ConcurrentDictionary<Guid, NetworkingProtocol> selfProtocols;
        private readonly ConcurrentDictionary<Guid, ClientServerProtocolEntry> peerProtocols;
        private readonly Dictionary<string, Func<NetworkingProtocol>> protocolInstantiators;
        private readonly List<ClientServerProtocolEntry> entriesToCheckAlive;

        [BindService]
        private XrvService xrvService = null;

        private NetworkSystem networking = null;
        private ClientServerProtocolEntry entryBeingUpdated;
        private TimeSpan currentCheckAliveProtocolsDelay;
        private ILogger logger;

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
            _ = Task.Run(this.UpdateProtocolEntries)
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            this.logger.LogError($"Error checking protocol orchestator alive entries: {t.Exception}");
                        }
                    });
        }

        internal void HandleIncomingMessage(IIncomingMessage message, bool receivedAsServer)
        {
            using (this.logger?.BeginScope("Protocol orchestator"))
            using (this.logger?.BeginScope("Incomming message"))
            {
                if (!message.IsProtocol)
                {
                    this.logger?.LogWarning($"Not a protocol message, skip network message");
                    return;
                }

                using (this.logger?.BeginScope("{CorrelationId}", message.CorrelationId))
                {
                    this.logger?.LogDebug($"Received message {message.LifecycleType}");

                    switch (message.LifecycleType)
                    {
                        case LifecycleMessageType.StartProtocol:
                            this.HandleProtocolStart(message.CorrelationId, message);
                            break;
                        case LifecycleMessageType.StartProtocolDenied:
                        case LifecycleMessageType.StartProtocolAccepted:
                            this.HandleProtocolStartResponse(message.CorrelationId, message);
                            break;
                        case LifecycleMessageType.AreYouStillAlive:
                            this.HandleAreYouStillAlive(message.CorrelationId, message);
                            break;
                        case LifecycleMessageType.ImStillAlive:
                            this.HandleImStillAlive(message.CorrelationId, message);
                            break;
                        case LifecycleMessageType.Talking:
                            this.HandleTalking(message.CorrelationId, message);
                            break;
                        case LifecycleMessageType.EndProtocol:
                            this.HandleProtocolEnd(message.CorrelationId, message);
                            break;
                    }
                }
            }
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
                this.networking = this.xrvService.Networking;
            }

            return attached;
        }

        private void HandleProtocolStart(Guid correlationId, IIncomingMessage message)
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
                this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolDenied, message.Sender.Id, deniedMessage.WriteTo);
                this.logger?.LogWarning($"Received protocol start message for an already started protocol. {LifecycleMessageType.StartProtocolDenied} signal sent.");

                return;
            }

            // Instantiate protocol
            if (!this.protocolInstantiators.ContainsKey(request.ProtocolName))
            {
                var deniedMessage = new StartProtocolResponseMessage
                {
                    ErrorCode = ProtocolError.MissingProtocolInstantiator,
                };
                this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolDenied, message.Sender.Id, deniedMessage.WriteTo);
                this.logger?.LogWarning($"Protocol instantiator not found for class name: {request.ProtocolName}. {LifecycleMessageType.StartProtocolDenied} signal sent.");

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
                protocolInstance.ProtocolStarter = new ProtocolStarter(protocolInstance, this.networking, this.lifecycle)
                {
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
            this.logger?.LogDebug($"Protocol {protocolInstance.Name} added to internal table.");

            var successMessage = new StartProtocolResponseMessage();
            this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.StartProtocolAccepted, entry.Sender.Id, successMessage.WriteTo);
            this.logger?.LogDebug($"{LifecycleMessageType.StartProtocolAccepted} signal sent to {entry.Sender.Id} for {protocolInstance.Name}");
        }

        private void HandleProtocolStartResponse(Guid correlationId, IIncomingMessage message)
        {
            NetworkingProtocol protocol;
            if (!this.selfProtocols.TryGetValue(correlationId, out protocol))
            {
                this.logger?.LogWarning("Received protocol start response message, but not found correlation. Skip message.");
                return;
            }

            var response = new StartProtocolResponseMessage
            {
                CorrelationId = correlationId,
            };
            message.To(response);

            this.logger?.LogDebug($"Delegating response message {protocol.Name} to {nameof(ProtocolStarter)}");
            protocol.ProtocolStarter.OnProtocolStartResponse(response);
        }

        private void HandleAreYouStillAlive(Guid correlationId, IIncomingMessage message)
        {
            // Check that sender is related with correlationId
            NetworkingProtocol protocol;
            if (!this.selfProtocols.TryGetValue(correlationId, out protocol))
            {
                this.logger?.LogWarning($"Received {LifecycleMessageType.AreYouStillAlive} message, but not found correlation");
                return;
            }

            // Send back ImStillAlive command
            this.lifecycle.SendLifecycleMessageToClient(correlationId, LifecycleMessageType.ImStillAlive, message.Sender.Id);
            this.logger?.LogDebug($"{LifecycleMessageType.ImStillAlive} signal sent to {message.Sender.Id} for {protocol.Name}");
        }

        private void HandleImStillAlive(Guid correlationId, IIncomingMessage message)
        {
            // Check that sender is related with correlationId
            ClientServerProtocolEntry entry;
            if (!this.peerProtocols.TryGetValue(correlationId, out entry))
            {
                this.logger?.LogWarning($"Received {LifecycleMessageType.ImStillAlive} message, but not found correlation");
                return;
            }

            if (entry.Sender.Id != message.Sender.Id)
            {
                this.logger?.LogWarning($"Received {LifecycleMessageType.ImStillAlive} message, but not from expected sender: {message.Sender.Id} ({entry.Sender.Id} expected)");
                return;
            }

            // Update last alive date
            entry.LastAliveDate = DateTime.UtcNow;
        }

        private void HandleTalking(Guid correlationId, IIncomingMessage message)
        {
            NetworkingProtocol protocol;

            if (!this.selfProtocols.TryGetValue(correlationId, out protocol))
            {
                // Check that sender is related with correlationId
                ClientServerProtocolEntry entry;

                if (!this.peerProtocols.TryGetValue(correlationId, out entry))
                {
                    this.logger?.LogWarning($"Received {LifecycleMessageType.Talking} message, but not found correlation");
                    return;
                }

                if (entry.Sender.Id != message.Sender.Id)
                {
                    this.logger?.LogWarning($"Received {LifecycleMessageType.Talking} message, but not from expected sender: {message.Sender.Id} ({entry.Sender.Id} expected)");
                    return;
                }

                protocol = entry.Protocol;

                // Update last alive date
                entry.LastAliveDate = DateTime.UtcNow;
            }

            if (protocol != null)
            {
                // Delegate in protocol
                INetworkingMessageConverter protocolMessageInstance = protocol.InternalCreateMessageInstance(message);
                this.logger?.LogDebug($"Delegating in protocol logic {protocol.Name}");

                protocol.InternalMessageReceived(protocolMessageInstance, message.Sender.Id);
            }
        }

        private void HandleProtocolEnd(Guid correlationId, IIncomingMessage message)
        {
            bool isAnyPeerProtocol = this.peerProtocols.ContainsKey(correlationId);
            bool isAnySelfProtocol = this.selfProtocols.ContainsKey(correlationId);

            if (!isAnyPeerProtocol && !isAnySelfProtocol)
            {
                this.logger?.LogWarning($"Received {LifecycleMessageType.EndProtocol} message for an unknown protocol");
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

        private void UpdateProtocolEntries()
        {
            using (this.logger?.BeginScope("Protocol orchestator"))
            using (this.logger?.BeginScope("Updating protocol entries"))
            {
                // Remove previously entries detected as required to be check as alive
                // that are still with a too old alive date
                var lastValidAliveDate = this.networking?.DebuggingEnabled ?? false
                    ? DateTime.MinValue
                    : DateTime.UtcNow - this.CheckAliveProtocolsDelay;

                for (int i = 0; i < this.entriesToCheckAlive.Count; i++)
                {
                    this.entryBeingUpdated = this.entriesToCheckAlive[i];
                    this.logger?.LogDebug($"Checking candidate to be removed: {this.entryBeingUpdated.CorrelationId}");

                    if (this.entryBeingUpdated.LastAliveDate < lastValidAliveDate)
                    {
                        this.logger?.LogDebug($"Removing: {this.entryBeingUpdated.CorrelationId}");
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
                    this.logger?.LogDebug($"Requesting alive echo for {this.entryBeingUpdated.CorrelationId}");
                    this.lifecycle.SendLifecycleMessageToClient(
                        this.entryBeingUpdated.CorrelationId,
                        LifecycleMessageType.AreYouStillAlive,
                        this.entryBeingUpdated.Sender.Id);
                }

                if (this.entriesToCheckAlive.Any())
                {
                    this.StillAliveSent?.Invoke(this, EventArgs.Empty);
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
