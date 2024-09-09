// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Raised when key store can not hold more keys.
    /// </summary>
    public class FullKeyStoreException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FullKeyStoreException"/> class.
        /// </summary>
        public FullKeyStoreException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FullKeyStoreException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public FullKeyStoreException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FullKeyStoreException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public FullKeyStoreException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
