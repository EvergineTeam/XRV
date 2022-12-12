// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.KeyRequest.Messages
{
    internal enum RequestKeyMessageType : byte
    {
        ClientRequestKeys, // client request a number of keys
        ServerRejectsKeysRequest, // server denies keys (not enough free keys)
        ServerAcceptKeysRequest, // server accept keys, it markes a set of keys as reserved for some time, and notifies client
        ClientConfirmsKeysReservation, // client confirms reserved keys
        ServerRejectsKeysConfirmation, // server denies reserved keys (time expired)
        ServerConfirmsKeysConfirmation, // server confirms keys reservation
        ClientCancelsKeysReservation, // client cancels keys reservation (confirmed or not)
    }
}
