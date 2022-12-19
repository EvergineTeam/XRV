// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.KeyRequest.Messages
{
    internal enum RequestKeyMessageType : byte
    {
        /// <summary>
        /// Client request a number of keys.
        /// </summary>
        ClientRequestKeys,

        /// <summary>
        /// Server rejects client request, for example, if there are not free keys.
        /// </summary>
        ServerRejectsKeysRequest,

        /// <summary>
        /// Server accepts client request.
        /// </summary>
        ServerAcceptKeysRequest,

        /// <summary>
        /// Client confirms key reservation.
        /// </summary>
        ClientConfirmsKeysReservation,

        /// <summary>
        /// Server rejects keys confirmation, for example, if reservation time has expired.
        /// </summary>
        ServerRejectsKeysConfirmation,

        /// <summary>
        /// Server confirms keys confirmation.
        /// </summary>
        ServerConfirmsKeysConfirmation,

        /// <summary>
        /// Client cancels keys reservation.
        /// </summary>
        ClientCancelsKeysReservation,
    }
}
