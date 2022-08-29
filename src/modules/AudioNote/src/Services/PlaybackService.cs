using Evergine.Common.Audio;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xrv.AudioNote.Services
{
    public class PlaybackService : UpdatableService
    {
        [BindService]
        protected AudioDevice audioDevice;

        protected AudioSource audioSource;

        private AudioBuffer buffer;
        private WaveFormat format;

        public event EventHandler<TimeSpan> CurrentPosition;

        public event EventHandler AudioEnd;

        public bool IsPlaying { get; protected set; }

        public TimeSpan Position { get; protected set; } = TimeSpan.Zero;

        public TimeSpan Duration { get; protected set; } = TimeSpan.Zero;

        public async Task<bool> Load(Stream stream)
        {
            try
            {
                // TODO this is config of sample
                this.format = new WaveFormat(true, sampleRate: 22050, encoding: WaveFormatEncodings.PCM8);
                this.buffer = this.audioDevice.CreateAudioBuffer();
                this.audioSource.Stop();
                await this.buffer.FillAsync(stream, (int)stream.Length, this.format);
                this.Duration = this.buffer.Duration;
            }
            catch (Exception ex)
            {
                // TODO log exception
                return false;
            }

            return true;
        }

        private void AudioSource_BufferEnded(object sender, AudioBufferEventArgs e)
        {
            this.audioSource.BufferEnded -= this.AudioSource_BufferEnded;
            this.AudioEnd?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            this.buffer = null;
            this.audioSource.Stop();
            this.IsPlaying = false;
            this.Position = TimeSpan.Zero;
        }

        public void Play()
        {
            this.audioSource.EnqueueBuffer(this.buffer);
            this.audioSource.Play();
            this.audioSource.BufferEnded += this.AudioSource_BufferEnded;
            this.IsPlaying = true;
        }

        public void Stop()
        {
            this.audioSource.Stop();
            this.IsPlaying = false;
            this.Position = TimeSpan.Zero;
            this.CurrentPosition?.Invoke(this, this.Position);
            this.AudioEnd?.Invoke(this, EventArgs.Empty);
        }

        public override void Update(TimeSpan gameTime)
        {
            if (this.IsPlaying)
            {
                this.Position = this.audioSource.PlayPosition;
                this.CurrentPosition?.Invoke(this, this.Position);
            }
        }
    }
}
