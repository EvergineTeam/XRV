// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Audio;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Xrv.AudioNote.Services
{
    /// <summary>
    /// Playback updatable service.
    /// </summary>
    public class PlaybackService : UpdatableService
    {
        /// <summary>
        /// audio device.
        /// </summary>
        [BindService]
        protected AudioDevice audioDevice;

        /// <summary>
        /// assets Service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        /// <summary>
        /// audio Source.
        /// </summary>
        protected AudioSource audioSource;

        private AudioBuffer buffer;
        private WaveFormat format;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaybackService"/> class.
        /// </summary>
        public PlaybackService()
        {
            // TODO this is config of sample
            this.format = new WaveFormat(true, sampleRate: 22050, encoding: WaveFormatEncodings.PCM8);
        }

        /// <summary>
        /// On position changed.
        /// </summary>
        public event EventHandler<TimeSpan> OnPositionChanged;

        /// <summary>
        /// On audio end
        /// </summary>
        public event EventHandler OnAudioEnd;

        /// <summary>
        /// Gets or sets a value indicating whether isPlaying.
        /// </summary>
        public bool IsPlaying { get; protected set; }

        /// <summary>
        /// Gets or sets Position.
        /// </summary>
        public TimeSpan Position { get; protected set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets Duration.
        /// </summary>
        public TimeSpan Duration { get; protected set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets Volume.
        /// </summary>
        public float Volume { get; set; } = 0.5f;

        /// <summary>
        /// Loads an audio stream.
        /// </summary>
        /// <param name="stream">Audio stream.</param>
        /// <returns>A <see cref="Task{TResult}"/> true if audio is loaded.</returns>
        public async Task<bool> Load(Stream stream)
        {
            try
            {
                if (this.audioDevice != null && this.audioSource == null)
                {
                    this.audioSource = this.audioDevice.CreateAudioSource(this.format);
                    this.audioSource.Volume = this.Volume;
                }

                this.audioSource.Stop();
                this.audioSource.FlushBuffers();
                await Task.Delay(1); // TODO do real load from audio
                ////this.buffer = this.audioDevice.CreateAudioBuffer();
                ////await this.buffer.FillAsync(stream, (int)stream.Length, this.format);

                this.buffer = this.assetsService.Load<AudioBuffer>(AudioNoteResourceIDs.Audio.Sample);
                this.Duration = this.buffer.Duration;
            }
            catch (Exception)
            {
                // TODO log exception
                return false;
            }

            return true;
        }

        /// <summary>
        /// Play audio.
        /// </summary>
        public void Play()
        {
            this.audioSource.EnqueueBuffer(this.buffer);
            this.audioSource.Play();
            this.audioSource.BufferEnded += this.AudioSource_BufferEnded;
            this.IsPlaying = true;
        }

        /// <summary>
        /// Stop audio.
        /// </summary>
        public void Stop()
        {
            this.audioSource.Stop();
            this.IsPlaying = false;
            this.Position = TimeSpan.Zero;
            this.OnPositionChanged?.Invoke(this, this.Position);
            this.OnAudioEnd?.Invoke(this, EventArgs.Empty);
            this.audioSource.BufferEnded -= this.AudioSource_BufferEnded;
        }

        /// <inheritdoc/>
        public override void Update(TimeSpan gameTime)
        {
            if (this.IsPlaying)
            {
                this.Position = this.audioSource.PlayPosition;
                this.OnPositionChanged?.Invoke(this, this.Position);
            }
        }

        private void AudioSource_BufferEnded(object sender, AudioBufferEventArgs e)
        {
            this.audioSource.BufferEnded -= this.AudioSource_BufferEnded;
            this.OnAudioEnd?.Invoke(this, EventArgs.Empty);
        }
    }
}
