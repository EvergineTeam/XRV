// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Xrv.AudioNote.Models;

namespace Xrv.AudioNote.Messages
{
    /// <summary>
    /// Audio note delete message.
    /// </summary>
    public class AudioNoteDeleteMessage
    {
        /// <summary>
        /// Gets or sets data.
        /// </summary>
        public AudioNoteData Data { get; set; }

        /// <summary>
        /// Gets or sets data.
        /// </summary>
        public AudioNoteWindow Window { get; set; }
    }
}
