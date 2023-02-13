// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.Core.Services.Messaging;
using Evergine.Xrv.Core.Services.MixedReality;
using Evergine.Xrv.Core.Services.QR;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Services
{
    /// <summary>
    /// Shared services that can be used app wide.
    /// </summary>
    public class SharedServices
    {
        /// <summary>
        /// Gets application logger.
        /// </summary>
        public ILogger Logging { get; internal set; }

        /// <summary>
        /// Gets basic publisher-subscriber implementation.
        /// </summary>
        public PubSub Messaging { get; internal set; }

        /// <summary>
        /// Gets passthrough service instance.
        /// </summary>
        public PasstroughService Passthrough { get; internal set; }

        /// <summary>
        /// Gets scanning workflow for QR codes.
        /// </summary>
        public QrScanningFlow QrScanningFlow { get; internal set; }
    }
}
