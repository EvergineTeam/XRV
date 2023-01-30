// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Services.Logging
{
    /// <summary>
    /// File logging options.
    /// </summary>
    public class FileLoggingOptions
    {
        /// <summary>
        /// Gets or sets log file base name. It could be extended with a suffix like
        /// log file date.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets maximum file size for a log file.
        /// </summary>
        public long? MaxFileSize { get; set; }
    }
}
