// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Configuration for networking participants.
    /// </summary>
    public class ParticipantsConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether participants feature is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether participant head
        /// should be considered for tracking.
        /// </summary>
        public bool TrackHead { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether participant hands
        /// should be considered for tracking.
        /// </summary>
        public bool TrackHands { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether participant controllers
        /// should be considered for tracking.
        /// </summary>
        public bool TrackControllers { get; set; } = true;

        /// <summary>
        /// Gets or sets avatar parts factory. This can be configured over
        /// class inheritance to create custom logic to select desired
        /// 3D models.
        /// </summary>
        public AvatarPartsFactory PartsFactory { get; set; }
    }
}
