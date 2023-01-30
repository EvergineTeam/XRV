// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Networking.Messaging
{
    internal enum LifecycleMessageType
    {
        StartProtocol,
        StartProtocolDenied,
        StartProtocolAccepted,
        AreYouStillAlive,
        ImStillAlive,
        Talking,
        EndProtocol,
    }
}
