// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Runtime.Serialization;

namespace Xrv.Core.Networking.Properties.KeyRequest
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

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysReservationTimeExpiredException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected KeysReservationTimeExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
