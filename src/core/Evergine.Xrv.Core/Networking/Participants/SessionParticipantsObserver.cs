// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.XR;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Networking.Client;
using Evergine.Networking.Client.Players;
using Evergine.Networking.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Evergine.Components.XR.TrackXRDevice;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class SessionParticipantsObserver : Behavior
    {
        private readonly Dictionary<int, Entity> participantEntities = new Dictionary<int, Entity>();
        private readonly ConcurrentQueue<ParticipantAction> actionQueue = new ConcurrentQueue<ParticipantAction>();

        [BindService]
        private MatchmakingClientService client = null;

        [BindService]
        private XrvService xrv = null;

        private NetworkSystem networking;
        private SessionParticipants sessionParticipants;
        private Task queueProcessingTask;
        private CancellationTokenSource queueProcessingTcs;
        private ILogger logger;

        private enum ParticipantActionType
        {
            Add,
            Remove,
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.networking = this.xrv.Networking;
                this.logger = this.xrv.Services.Logging;
                this.sessionParticipants = this.networking.Participants;
                this.client.ClientStateChanged += this.Client_ClientStateChanged;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            this.client.ClientStateChanged -= this.Client_ClientStateChanged;
            this.networking = null;
            this.sessionParticipants = null;
            this.logger = null;
        }

        protected override void Update(TimeSpan gameTime)
        {
            if (this.actionQueue.Any() && this.queueProcessingTask == null)
            {
                this.queueProcessingTcs?.Cancel();
                this.queueProcessingTcs = new CancellationTokenSource();
                this.queueProcessingTask = this.ProcessQueueAsync(this.queueProcessingTcs.Token)
                    .ContinueWith(t =>
                    {
                        this.queueProcessingTask = null;

                        if (t.IsFaulted)
                        {
                            this.logger?.LogError(t.Exception, "Error processing participants queue");
                        }
                    });
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellation = default)
        {
            while (this.actionQueue.Any() && !cancellation.IsCancellationRequested)
            {
                ParticipantAction action;
                if (!this.actionQueue.TryDequeue(out action))
                {
                    break;
                }

                if (action.ActionType == ParticipantActionType.Add)
                {
                    await this.AddParticipantHierarchyAsync(action.ParticipantInfo).ConfigureAwait(false);
                }
                else
                {
                    await EvergineForegroundTask.Run(() => this.RemoveParticipantHierarchy(action.ParticipantInfo.ClientId))
                        .ConfigureAwait(false);
                }
            }
        }

        private void SubscribeToRoomEvents()
        {
            var room = this.client.CurrentRoom;
            if (room != null)
            {
                room.PlayerJoined += this.CurrentRoom_PlayerJoined;
                room.PlayerLeaving += this.CurrentRoom_PlayerLeaving;
            }
        }

        private void UnsubscribeFromRoomEvents()
        {
            var room = this.client.CurrentRoom;
            if (room != null)
            {
                room.PlayerJoined -= this.CurrentRoom_PlayerJoined;
                room.PlayerLeaving -= this.CurrentRoom_PlayerLeaving;
            }
        }

        private void EnqueueInitialParticipants()
        {
            using (this.logger?.BeginScope("Session participants queue"))
            {
                this.logger?.LogDebug($"Adding local participant: ID={this.client.LocalPlayer.Id} to queue");
                this.EnqueueParticipantAction(this.client.LocalPlayer.Id, true, ParticipantActionType.Add);

                foreach (var remote in this.client.CurrentRoom.RemotePlayers)
                {
                    this.logger?.LogDebug($"Adding remote participant: ID={remote.Id} to queue");
                    this.EnqueueParticipantAction(remote.Id, false, ParticipantActionType.Add);
                }
            }
        }

        private void EnqueueParticipantAction(int clientId, bool isLocal, ParticipantActionType actionType)
        {
            this.actionQueue.Enqueue(new ParticipantAction
            {
                ParticipantInfo = new ParticipantInfo
                {
                    ClientId = clientId,
                    IsLocalClient = isLocal,
                },
                ActionType = actionType,
            });
        }

        private async Task AddParticipantHierarchyAsync(ParticipantInfo participant)
        {
            var playerProvider = new NetworkPlayerProvider()
            {
                PlayerId = participant.ClientId,
            };

            var participantEntity = new Entity($"Networking-Participant-{participant.ClientId}")
                .AddComponent(new Transform3D())
                .AddComponent(playerProvider);

            await this.AddTrackingComponentsAsync(participant, participantEntity)
                .ConfigureEvergineAwait(EvergineTaskContinueOn.Foreground);
            this.participantEntities.Add(participant.ClientId, participantEntity);
            this.networking.AddNetworkingEntity(participantEntity);
        }

        private void RemoveParticipantHierarchy(int clientId)
        {
            if (this.participantEntities.ContainsKey(clientId))
            {
                var participantEntity = this.participantEntities[clientId];
                this.participantEntities.Remove(clientId);
                this.networking.RemoveNetworkingEntity(participantEntity);
            }
        }

        private void RemoveAllParticipantsFromHierarchy()
        {
            var allParticipants = this.participantEntities.Keys;
            foreach (var clientId in allParticipants)
            {
                this.RemoveParticipantHierarchy(clientId);
            }
        }

        private async Task AddTrackingComponentsAsync(ParticipantInfo participant, Entity parent)
        {
            var configuration = this.sessionParticipants.Configuration;
            if (configuration.TrackHead)
            {
                await this.AddTrackingComponentsByElementAsync(
                    participant,
                    parent,
                    TrackedElement.Head).ConfigureAwait(false);
            }

            if (configuration.TrackHands)
            {
                await this.AddTrackingComponentsByElementAsync(
                    participant,
                    parent,
                    TrackedElement.LeftHand).ConfigureAwait(false);
                await this.AddTrackingComponentsByElementAsync(
                    participant,
                    parent,
                    TrackedElement.RightHand).ConfigureAwait(false);
            }

            if (configuration.TrackControllers)
            {
                await this.AddTrackingComponentsByElementAsync(
                    participant,
                    parent,
                    TrackedElement.LeftController).ConfigureAwait(false);
                await this.AddTrackingComponentsByElementAsync(
                    participant,
                    parent,
                    TrackedElement.RightController).ConfigureAwait(false);
            }
        }

        private async Task AddTrackingComponentsByElementAsync(
            ParticipantInfo participant,
            Entity parent,
            TrackedElement element)
        {
            var configuration = this.sessionParticipants.Configuration;
            Entity rootEntity = new Entity()
                .AddComponent(new Transform3D());
            rootEntity.Name = element.ToString();

            var factory = configuration.PartsFactory;
            Entity visibleEntity = await factory.InstantiateElementAsync(participant, element);
            rootEntity.AddChild(visibleEntity);

            if (element == TrackedElement.Head)
            {
                rootEntity
                    .AddComponent(new TransformSynchronizationByClient
                    {
                        ProviderFilter = NetworkPropertyProviderFilter.Player,
                        PropertyKey = this.sessionParticipants.GetPropertyKeyByElement(element),
                    });

                if (participant.IsLocalClient)
                {
                    rootEntity
                        .AddComponent(new HeadTracking());
                }
            }
            else if (element.IsHandedness())
            {
                rootEntity
                    .AddComponent(new XRTrackableSynchronization
                    {
                        ProviderFilter = NetworkPropertyProviderFilter.Player,
                        PropertyKey = this.sessionParticipants.GetPropertyKeyByElement(element),
                    });

                if (participant.IsLocalClient)
                {
                    rootEntity
                        .AddComponent(new XRTrackableDisconnectionListener());

                    TrackXRDevice xrDevice = null;
                    XRTrackableObserver observer = null;
                    var handedness = element.GetHandedness();

                    if (element.IsHand())
                    {
                        xrDevice = new TrackXRArticulatedHand
                        {
                            Handedness = handedness,
                        };
                        observer = new XRTrackableHandObserver();
                    }
                    else if (element.IsController())
                    {
                        xrDevice = new TrackXRController
                        {
                            Handedness = handedness,
                        };
                        observer = new XRTrackableControllerObserver();
                    }

                    if (xrDevice != null && observer != null)
                    {
                        xrDevice.TrackingLostMode = XRTrackingLostMode.DisableEntityOnDisconnection;
                        observer.Handedness = handedness;
                        visibleEntity
                            .AddComponent(xrDevice)
                            .AddComponent(observer);
                    }
                }
            }

            parent.AddChild(rootEntity);
        }

        private void Client_ClientStateChanged(object sender, ClientStates state)
        {
            switch (state)
            {
                case ClientStates.Joined:
                    this.SubscribeToRoomEvents();
                    this.EnqueueInitialParticipants();
                    break;
                case ClientStates.Leaving:
                    this.UnsubscribeFromRoomEvents();
                    this.queueProcessingTcs?.Cancel();
                    this.queueProcessingTcs = null;
                    EvergineForegroundTask.Run(() => this.RemoveAllParticipantsFromHierarchy());
                    break;
            }
        }

        private void CurrentRoom_PlayerJoined(object sender, RemoteNetworkPlayer client)
        {
            this.logger?.LogDebug($"Adding remote participant: ID={client.Id} to queue");
            this.EnqueueParticipantAction(client.Id, false, ParticipantActionType.Add);
        }

        private void CurrentRoom_PlayerLeaving(object sender, RemoteNetworkPlayer client)
        {
            this.logger?.LogDebug($"Removing remote participant: ID={client.Id}");
            this.EnqueueParticipantAction(client.Id, false, ParticipantActionType.Remove);
        }

        private class ParticipantAction
        {
            public ParticipantInfo ParticipantInfo { get; set; }

            public ParticipantActionType ActionType { get; set; }
        }
    }
}
