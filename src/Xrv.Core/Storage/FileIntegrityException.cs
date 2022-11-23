// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Runtime.Serialization;

namespace Xrv.Core.Storage
{
    /// <summary>
    /// Raised when file integrity check is not satisfactory.
    /// </summary>
    public class FileIntegrityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileIntegrityException"/> class.
        /// </summary>
        public FileIntegrityException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileIntegrityException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public FileIntegrityException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileIntegrityException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public FileIntegrityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileIntegrityException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected FileIntegrityException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
