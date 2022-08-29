using Evergine.Framework.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xrv.AudioNote.Services
{
    public class RecordingService : UpdatableService
    {
        public bool IsRecording { get; protected set; }

        public TimeSpan RecordingTime { get; protected set; }

        public Stream BufferStream { get; protected set; }

        public event EventHandler IsRecordingChanged;

        public event EventHandler RecordingTimeChanged;

        public override void Update(TimeSpan gameTime)
        {
            //throw new NotImplementedException();
        }

        public Task<bool> StartRecordingAsync()
        {
            return Task.FromResult(true);
        }

        public Task<bool> StopRecordingAsync()
        {
            return Task.FromResult(true);
        }
    }
}
