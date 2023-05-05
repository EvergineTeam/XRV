// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using System.Collections.Generic;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Configuration for networking participants.
    /// </summary>
    public class ParticipantsConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParticipantsConfiguration"/> class.
        /// </summary>
        public ParticipantsConfiguration()
        {
            this.AvatarTintColors = new List<Color>
            {
                Color.Cyan,
                Color.Yellow,
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.White,
                Color.Orange,
                Color.Purple,
            };
        }

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

        /// <summary>
        /// Gets or sets available list of colors for avatar tinting. Collection can be
        /// modified programmatically to change default color palette.
        /// </summary>
        public List<Color> AvatarTintColors { get; set; }
    }
}
