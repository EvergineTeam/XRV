// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Animation;
using Evergine.Framework;
using System;

namespace Xrv.Core.HandTutorial
{
    /// <summary>
    /// Execute a ping pong animation.
    /// </summary>
    public partial class PingPongAnimation : Behavior
    {
        [BindComponent]
        private Animation3D animation = null;

        private TimeSpan countdown = TimeSpan.FromSeconds(0.5f);
        private TimeSpan time;
        private States currentState;
        private AnimationTrackClip clip = null;

        private enum States
        {
            PlayForward,
            PlayBackwards,
            WaitingForward,
            WaitingBackwards,
        }

        /// <summary>
        /// Gets or sets the Animation name.
        /// </summary>
        public string AnimationName { get; set; }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.clip = new AnimationTrackClip(this.animation.Model.Animations[this.AnimationName]);
            this.currentState = States.PlayForward;
            this.animation.PlayAnimation(this.clip);
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            switch (this.currentState)
            {
                case States.PlayForward:
                    if (this.animation.Clip.Phase >= 1)
                    {
                        this.currentState = States.WaitingBackwards;
                        this.time = this.countdown;
                    }

                    break;

                case States.WaitingBackwards:

                    this.time -= gameTime;
                    if (this.time <= TimeSpan.Zero)
                    {
                        this.currentState = States.PlayBackwards;
                        this.clip.PlaybackRate = -1;
                        this.animation.PlayAnimation(this.clip);
                    }

                    break;
                case States.PlayBackwards:

                    if (this.animation.Clip.Phase <= 0)
                    {
                        this.currentState = States.WaitingForward;
                        this.time = this.countdown;
                    }

                    break;

                case States.WaitingForward:

                    this.time -= gameTime;
                    if (this.time <= TimeSpan.Zero)
                    {
                        this.currentState = States.PlayForward;
                        this.clip.PlaybackRate = 1;
                        this.animation.PlayAnimation(this.clip);
                    }

                    break;
            }
        }
    }
}
