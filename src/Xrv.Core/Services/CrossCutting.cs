// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Microsoft.Extensions.Logging;
using Xrv.Core.Services.QR;

namespace Xrv.Core.Services
{
    /// <summary>
    /// Cross-cutting services that can be used app wide.
    /// </summary>
    public class CrossCutting
    {
        /// <summary>
        /// Gets application logger.
        /// </summary>
        public ILogger Logging { get; internal set; }

        /// <summary>
        /// Gets scanning workflow for QR codes.
        /// </summary>
        public QrScanningFlow QrScanningFlow { get; internal set; }
    }
}
