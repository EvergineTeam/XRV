// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking
{
    /// <summary>
    /// Network configuration builder.
    /// </summary>
    public class NetworkConfigurationBuilder
    {
        private readonly NetworkConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkConfigurationBuilder"/> class.
        /// </summary>
        public NetworkConfigurationBuilder()
        {
            if (!NetworkSystem.NetworkSystemEnabled)
            {
                throw new NotSupportedException("Networking not available");
            }

            this.configuration = new NetworkConfiguration()
            {
                ClientApplicationVersion = "1.0.0",
                Port = 12345,
                PingInterval = TimeSpan.FromSeconds(4),
                ConnectionTimeout = TimeSpan.FromSeconds(8),
                ServerScanInterval = TimeSpan.FromSeconds(5),
            };
        }

        /// <summary>
        /// Sets an application identifier for builder.
        /// </summary>
        /// <param name="applicationId">Application identifier.</param>
        /// <returns>Builder.</returns>
        /// <exception cref="ArgumentNullException">Application identifier is null.</exception>
        public NetworkConfigurationBuilder ForApplication(string applicationId)
        {
            if (string.IsNullOrEmpty(applicationId))
            {
                throw new ArgumentNullException(nameof(applicationId));
            }

            this.configuration.ApplicationIdentifier = applicationId;
            return this;
        }

        /// <summary>
        /// Sets an application version for builder.
        /// </summary>
        /// <param name="applicationVersion">Application version.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder ForVersion(string applicationVersion)
        {
            this.configuration.ClientApplicationVersion = applicationVersion;
            return this;
        }

        /// <summary>
        /// Sets port for the client/server. If both client and server are going
        /// to coexist in the same host machine, remember to set different port numbers, as
        /// same port can't be used twice in the same host.
        /// </summary>
        /// <param name="port">Port number.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder UsePort(int port)
        {
            this.configuration.Port = port;
            return this;
        }

        /// <summary>
        /// Sets ping interval for builder.
        /// </summary>
        /// <param name="interval">Ping interval.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder PingEach(TimeSpan interval)
        {
            this.configuration.PingInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets connection timeout interval for builder.
        /// </summary>
        /// <param name="timeout">Connection timeout.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder WithTimeout(TimeSpan timeout)
        {
            this.configuration.ConnectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets scan interval for clients looking for a server.
        /// </summary>
        /// <param name="interval">Scan interval.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder WithScanInterval(TimeSpan interval)
        {
            this.configuration.ServerScanInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets QR code to be scanned to join an existing session.
        /// </summary>
        /// <param name="qrCode">Target QR code.</param>
        /// <returns>Builder.</returns>
        public NetworkConfigurationBuilder SetQrCodeForSession(string qrCode)
        {
            this.configuration.QrSessionCode = qrCode;
            return this;
        }

        /// <summary>
        /// Builds the configuration.
        /// </summary>
        /// <returns>Network configuration.</returns>
        /// <exception cref="InvalidOperationException">If <see cref="ForApplication"/> has not been invoked.</exception>
        public NetworkConfiguration Build()
        {
            if (string.IsNullOrEmpty(this.configuration.ApplicationIdentifier))
            {
                throw new InvalidOperationException($"You must set a value for {nameof(this.configuration.ApplicationIdentifier)}");
            }

            return this.configuration;
        }
    }
}
