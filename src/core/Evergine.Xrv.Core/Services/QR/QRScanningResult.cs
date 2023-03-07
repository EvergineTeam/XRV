// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.XR.QR;
using Evergine.Mathematics;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// QR scanning result data.
    /// </summary>
    public class QRScanningResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QRScanningResult"/> class.
        /// </summary>
        /// <param name="code">QR code.</param>
        /// <param name="pose">QR pose.</param>
        public QRScanningResult(QRCode code, Matrix4x4 pose)
        {
            this.Code = code;
            this.Pose = pose;
        }

        /// <summary>
        /// Gets QR code data.
        /// </summary>
        public QRCode Code { get; private set; }

        /// <summary>
        /// Gets QR code pose.
        /// </summary>
        public Matrix4x4 Pose { get; private set; }
    }
}
