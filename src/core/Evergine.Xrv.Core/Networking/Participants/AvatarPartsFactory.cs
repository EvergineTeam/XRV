﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK;
using System;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Factory to create avatar parts entities.
    /// </summary>
    public class AvatarPartsFactory
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        protected readonly AssetsService AssetsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarPartsFactory"/> class.
        /// </summary>
        /// <param name="assetsService">Assets service.</param>
        public AvatarPartsFactory(AssetsService assetsService)
        {
            this.AssetsService = assetsService;
        }

        internal virtual async Task<Entity> InstantiateElementAsync(ParticipantInfo participant, TrackedElement element)
        {
            if (participant.IsLocalClient)
            {
                var emptyEntity = new Entity()
                    .AddComponent(new Transform3D());
                return emptyEntity;
            }

            var prefabId = this.GetPrefabIdByElement(participant, element);
            var entity = await this.CreateEntityFromPrefabAsync(prefabId).ConfigureAwait(false);

            if (element == TrackedElement.Head)
            {
                entity.FindComponentInChildren<AvatarDisplayName>().Nickname = participant.Nickname;
            }

            // 3D elements tinting
            if (element.IsController())
            {
                var colorMarkEntity = new Entity("colorMark")
                    .AddComponent(new Transform3D
                    {
                        LocalPosition = new Vector3(0f, 0.045f, 0.032f),
                        LocalScale = new Vector3(0.02f, 0.02f, 0.02f),
                    })
                    .AddComponent(new MaterialComponent
                    {
                        UseCopy = true,
                        Material = this.AssetsService.Load<Material>(CoreResourcesIDs.Materials.Networking.Participants.ControllerMarker),
                    })
                    .AddComponent(new SphereMesh())
                    .AddComponent(new MeshRenderer())
                    .AddComponent(new AvatarTintColor());
                entity.AddChild(colorMarkEntity);
            }

            var tintComponents = entity.FindComponentsInChildren<AvatarTintColor>();
            foreach (var tint in tintComponents)
            {
                tint.TintColor = participant.AvatarColor;
            }

            return entity;
        }

        /// <summary>
        /// Creates an entity from a prefab, in a background job.
        /// </summary>
        /// <param name="prefabId">Prefab ID.</param>
        /// <returns>Instantiated prefab entity.</returns>
        protected Task<Entity> CreateEntityFromPrefabAsync(Guid prefabId)
        {
            var taskCompletionSource = new TaskCompletionSource<Entity>();
            EvergineBackgroundTask.Run(() =>
            {
                try
                {
                    var prefab = this.AssetsService.Load<Prefab>(prefabId);
                    var entity = prefab.Instantiate();
                    taskCompletionSource.TrySetResult(entity);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.TrySetException(ex);
                }
            });

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Gets prefab ID for given participant and tracked element type.
        /// </summary>
        /// <param name="participant">Participant.</param>
        /// <param name="element">Tracked element.</param>
        /// <returns>Prefab ID.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Raised for not supported elements.</exception>
        protected virtual Guid GetPrefabIdByElement(ParticipantInfo participant, TrackedElement element)
        {
            switch (element)
            {
                case TrackedElement.Head:
                    return participant.DeviceInfo?.IsHoloLens() ?? false
                        ? CoreResourcesIDs.Prefabs.Networking.Participants.HoloLensHead_weprefab
                        : CoreResourcesIDs.Prefabs.Networking.Participants.DefaultHead_weprefab;
                case TrackedElement.LeftHand:
                    return CoreResourcesIDs.Prefabs.Networking.Participants.leftPalm_weprefab;
                case TrackedElement.RightHand:
                    return CoreResourcesIDs.Prefabs.Networking.Participants.rightPalm_weprefab;
                case TrackedElement.LeftController:
                    return MRTKResourceIDs.Prefabs.DefaultLeftController_weprefab;
                case TrackedElement.RightController:
                    return MRTKResourceIDs.Prefabs.DefaultRightController_weprefab;
                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }
        }
    }
}