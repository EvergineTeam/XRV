// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.XR.QR;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// Event args for QR scanning result.
    /// </summary>
    public class QRScanningResultEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QRScanningResultEventArgs"/> class.
        /// </summary>
        /// <param name="isValidResult">Indicates if scanned code was valid.</param>
        /// <param name="code">QR code data.</param>
        /// <param name="pose">QR code pose.</param>
        public QRScanningResultEventArgs(bool isValidResult, QRCode code, Matrix4x4 pose)
        {
            this.IsValidResult = isValidResult;
            this.Code = code;
            this.Pose = pose;
        }

        /// <summary>
        /// Gets a value indicating whether detection result is valid.
        /// </summary>
        public bool IsValidResult { get; private set; }

        /// <summary>
        /// Gets detected QR code data.
        /// </summary>
        public QRCode Code { get; private set; }

        /// <summary>
        /// Gets detected QR code pose.
        /// </summary>
        public Matrix4x4 Pose { get; private set; }
    }
}
