// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Storage
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
    }
}
