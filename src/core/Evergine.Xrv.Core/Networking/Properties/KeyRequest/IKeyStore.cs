// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Components;
using System;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Networking properties key store.
    /// </summary>
    internal interface IKeyStore
    {
        /// <summary>
        /// Gets or sets time that a key is reserved until it is confirmed.
        /// If not confirmed, key will be available again.
        /// </summary>
        TimeSpan KeyReservationTime { get; set; }

        /// <summary>
        /// Reserves an amount of keys.
        /// </summary>
        /// <param name="numberOfKeys">Number of keys to be reserved.</param>
        /// <param name="correlationId">Protocol correlation identifier.</param>
        /// <param name="ownerClientId">Client that makes the reservation.</param>
        /// <param name="filter">Property type.</param>
        /// <returns>An array with reserved keys.</returns>
        KeyRegister[] ReserveKeys(
            byte numberOfKeys,
            Guid correlationId,
            int ownerClientId,
            NetworkPropertyProviderFilter filter);

        /// <summary>
        /// Confirms keys reservation.
        /// </summary>
        /// <param name="correlationId">Protocol correlation identifier.</param>
        /// <param name="ownerClientId">Client that makes the reservation.</param>
        void ConfirmKeys(Guid correlationId, int ownerClientId);

        /// <summary>
        /// Frees some previously reserved keys.
        /// </summary>
        /// <param name="correlationId">Protocol correlation identifier.</param>
        /// <param name="ownerClientId">Client that makes the reservation.</param>
        void FreeKeys(Guid correlationId, int ownerClientId);

        /// <summary>
        /// Clears store.
        /// </summary>
        void Clear();
    }
}
