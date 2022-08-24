using Xrv.AudioNote.Models;

namespace Xrv.AudioNote.Messages
{
    public class AudioNoteMessage
    {
        public AudioNoteData Data { get; set; }
        public AudioNoteAnchorState State { get; set; }
    }
}
