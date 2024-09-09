// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Raised when protocol timeout is reached.
    /// </summary>
    public class ProtocolTimeoutException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolTimeoutException"/> class.
        /// </summary>
        public ProtocolTimeoutException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolTimeoutException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ProtocolTimeoutException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolTimeoutException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProtocolTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
