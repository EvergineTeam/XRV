// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Microsoft.Extensions.Logging;

namespace Xrv.Core.Services.Logging
{
    /// <summary>
    /// Logging configuration.
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>
        /// Gets or sets log level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets a value indicating whether file logging is enabled.
        /// </summary>
        public bool EnableFileLogging { get => this.FileOptions != null; }

        /// <summary>
        /// Gets or sets file logging options.
        /// </summary>
        public FileLoggingOptions FileOptions { get; set; }
    }
}
