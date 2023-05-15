// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.UI.Notifications
{
    /// <summary>
    /// Notification configuration model.
    /// </summary>
    public class NotificationConfiguration
    {
        /// <summary>
        /// Gets or sets target icon material identifier.
        /// </summary>
        public Guid IconMaterial { get; set; }

        /// <summary>
        /// Gets or sets notification title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets notification text.
        /// </summary>
        public string Text { get; set; }
    }
}
