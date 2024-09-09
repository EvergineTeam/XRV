// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Raised when there is an attempt to confirm keys which
    /// reservation time has expired.
    /// </summary>
    public class KeysReservationTimeExpiredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeysReservationTimeExpiredException"/> class.
        /// </summary>
        public KeysReservationTimeExpiredException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysReservationTimeExpiredException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeysReservationTimeExpiredException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysReservationTimeExpiredException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeysReservationTimeExpiredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
