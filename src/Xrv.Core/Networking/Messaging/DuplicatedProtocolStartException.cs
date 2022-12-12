// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Runtime.Serialization;

namespace Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Raised when a protocol attempts to be registered twice.
    /// </summary>
    public class DuplicatedProtocolStartException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicatedProtocolStartException"/> class.
        /// </summary>
        public DuplicatedProtocolStartException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicatedProtocolStartException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public DuplicatedProtocolStartException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicatedProtocolStartException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public DuplicatedProtocolStartException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicatedProtocolStartException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected DuplicatedProtocolStartException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
