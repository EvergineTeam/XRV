// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xrv.AudioNote.Services
{
    /// <summary>
    /// Recording updatable service.
    /// </summary>
    public class RecordingService : UpdatableService
    {
        /// <summary>
        /// On recording time changed
        /// </summary>
        public event EventHandler<TimeSpan> OnRecordingTime;

        /// <summary>
        /// Gets or sets a value indicating whether indicates if is recording.
        /// </summary>
        public bool IsRecording { get; protected set; }

        /// <summary>
        /// Gets or sets current Recording Time.
        /// </summary>
        public TimeSpan RecordingTime { get; protected set; }

        /// <inheritdoc/>
        public override void Update(TimeSpan gameTime)
        {
            if (this.IsRecording)
            {
                this.RecordingTime += gameTime;
                this.OnRecordingTime?.Invoke(this, this.RecordingTime);
            }
        }

        /// <summary>
        /// Start recording async.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> true if ok.</returns>
        public async Task<bool> StartRecordingAsync()
        {
            this.IsRecording = true;
            this.RecordingTime = TimeSpan.Zero;
            await Task.Delay(1);
            return true;
        }

        /// <summary>
        /// Stop recording async.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> Stream with data recorded.</returns>
        public async Task<Stream> StopRecordingAsync()
        {
            this.IsRecording = false;
            await Task.Delay(1);
            return new MemoryStream();
        }
    }
}
