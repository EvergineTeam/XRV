// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Xrv.Core.Services.QR;

namespace Xrv.Core.Services
{
    /// <summary>
    /// Cross-cutting services that can be used app wide.
    /// </summary>
    public class CrossCutting
    {
        /// <summary>
        /// Gets scanning workflow for QR codes.
        /// </summary>
        public QrScanningFlow QrScanningFlow { get; internal set; }
    }
}
