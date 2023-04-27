// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Data object for participant information.
    /// </summary>
    public class ParticipantInfo
    {
        /// <summary>
        /// Gets networking client identifier.
        /// </summary>
        public int ClientId { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether client is the one created
        /// by running device. This is, the user itself.
        /// </summary>
        public bool IsLocalClient { get; internal set; }
    }
}
