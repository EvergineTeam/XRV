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

        public event EventHandler<TimeSpan> RecordingTimeChanged;

        public override void Update(TimeSpan gameTime)
        {
            if (this.IsRecording)
            {
                this.RecordingTime += gameTime;
                this.RecordingTimeChanged?.Invoke(this, this.RecordingTime);
            }
        }

        public async Task<bool> StartRecordingAsync()
        {
            this.IsRecording = true;
            this.RecordingTime = TimeSpan.Zero;
            await Task.Delay(1);
            return true;
        }

        public async Task<Stream> StopRecordingAsync()
        {
            this.IsRecording = false;
            await Task.Delay(1);
            return new MemoryStream();
        }
    }
}
