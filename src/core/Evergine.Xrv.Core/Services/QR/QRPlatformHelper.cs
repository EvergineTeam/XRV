// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Mathematics;
using Evergine.Xrv.Core.Utils;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// Platform-specific helper to work with QR codes.
    /// </summary>
    public static class QRPlatformHelper
    {
        /// <summary>
        /// Fixes code origin to match with QR prefab coordinates system.
        /// </summary>
        /// <param name="position">QR representation position.</param>
        public static void FixUpCodeOrigin(ref Vector3 position)
        {
            /* As stated here
             * https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/qr-code-tracking-unity#getting-the-coordinate-system-for-a-qr-code
             *
             * HoloLens API coordinate system considers top-left corner of QR code as origin.
             * Our prefab origin is in the center (0.5, 0.5). We don't change this because maybe, in the future,
             * we integrate other platforms for QR scanning, and maybe those platforms returns QR position from its center.
             */

            if (DeviceHelper.IsHoloLens())
            {
                position.X = position.Z = 0.5f;
            }
        }
    }
}
