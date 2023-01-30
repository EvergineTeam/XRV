// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Runtime.Serialization;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Raised when confirmed keys are no longer available.
    /// </summary>
    public class KeysToConfirmNotAvailableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeysToConfirmNotAvailableException"/> class.
        /// </summary>
        public KeysToConfirmNotAvailableException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysToConfirmNotAvailableException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeysToConfirmNotAvailableException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysToConfirmNotAvailableException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeysToConfirmNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeysToConfirmNotAvailableException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected KeysToConfirmNotAvailableException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
