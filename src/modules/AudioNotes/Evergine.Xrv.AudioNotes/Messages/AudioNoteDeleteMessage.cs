// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Xrv.AudioNotes.Models;

namespace Evergine.Xrv.AudioNotes.Messages
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
