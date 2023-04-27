// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Services;
using Evergine.Networking.Components;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using System;
using System.Collections.Generic;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Controls session participants networking feature, that shows
    /// a 3D representation of session participants. Poses of different
    /// elements like head or hands will be shared to rest of users.
    /// </summary>
    public class SessionParticipants
    {
        private readonly KeyStore keyStore;
        private readonly Dictionary<TrackedElement, byte> assignedKeys;

        internal SessionParticipants(KeyStore keyStore, AssetsService assetsService)
        {
            this.keyStore = keyStore;
            this.assignedKeys = new Dictionary<TrackedElement, byte>();
            this.Configuration = new ParticipantsConfiguration
            {
                PartsFactory = new AvatarPartsFactory(assetsService),
            };
        }

        /// <summary>
        /// Gets or sets tracking configuration.
        /// </summary>
        public ParticipantsConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets networking property keys that are reserved for each
        /// client synchronization table, to make avatar parts work.
        /// </summary>
        /// <param name="element">Tracked element.</param>
        /// <returns>Property key associated with the element.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Raised when asked tracked
        /// element does not have a registered key.</exception>
        public byte GetPropertyKeyByElement(TrackedElement element)
        {
            if (!this.assignedKeys.ContainsKey(element))
            {
                throw new ArgumentOutOfRangeException(nameof(element));
            }

            return this.assignedKeys[element];
        }

        internal void Load(Entity worldCenterEntity)
        {
            if (this.Configuration?.IsEnabled != true)
            {
                return;
            }

            var keys = new List<byte>();
            byte currentKey = 0x01;

            if (this.Configuration.TrackHead)
            {
                this.assignedKeys.Add(TrackedElement.Head, currentKey);
                currentKey++;
            }

            if (this.Configuration.TrackHands)
            {
                this.assignedKeys.Add(TrackedElement.LeftHand, currentKey);
                currentKey++;

                this.assignedKeys.Add(TrackedElement.RightHand, currentKey);
                currentKey++;
            }

            if (this.Configuration.TrackControllers)
            {
                this.assignedKeys.Add(TrackedElement.LeftController, currentKey);
                currentKey++;

                this.assignedKeys.Add(TrackedElement.RightController, currentKey);
                currentKey++;
            }

            this.keyStore.ReserveKeysForCore(keys.ToArray(), NetworkPropertyProviderFilter.Player);

            // listen to incoming/outcoming session participants
            worldCenterEntity.AddComponent(new SessionParticipantsObserver());
        }
    }
}
