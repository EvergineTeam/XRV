// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Protocol lifecycle flow errors.
    /// </summary>
    public enum ProtocolError : byte
    {
        /// <summary>
        /// Correlation identifier already in use.
        /// </summary>
        DuplicatedCorrelationId = 1,

        /// <summary>
        /// Protocol instantiator has not been registered.
        /// </summary>
        MissingProtocolInstantiator,
    }
}
