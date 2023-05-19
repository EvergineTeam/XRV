// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Network configuration model.
    /// </summary>
    public class NetworkConfiguration
    {
        /// <summary>
        /// Gets network application identifier. Should be set
        /// with the same values in both server and client in order to work.
        /// </summary>
        public string ApplicationIdentifier { get; internal set; }

        /// <summary>
        /// Gets network application version. Should be set
        /// with the same values in both server and client in order to work.
        /// </summary>
        public string ClientApplicationVersion { get; internal set; }

        /// <summary>
        /// Gets port to be used by the client.
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Gets ping interval that clients will apply to check
        /// their connection status.
        /// </summary>
        public TimeSpan PingInterval { get; internal set; }

        /// <summary>
        /// Gets timeout interval until a client-server connection is
        /// considered to still be alive.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; internal set; }

        /// <summary>
        /// Gets scan time for clients while looking for a network server.
        /// </summary>
        public TimeSpan ServerScanInterval { get; internal set; }

        /// <summary>
        /// Gets code to be scanned by QR scanning flow for
        /// session connection.
        /// </summary>
        public string QrSessionCode { get; internal set; }
    }
}
