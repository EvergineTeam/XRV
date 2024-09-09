// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Raised when a protocol is attempt to be used but has not been
    /// registered.
    /// </summary>
    public class ProtocolNotRegisteredException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolNotRegisteredException"/> class.
        /// </summary>
        public ProtocolNotRegisteredException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolNotRegisteredException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ProtocolNotRegisteredException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtocolNotRegisteredException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ProtocolNotRegisteredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
