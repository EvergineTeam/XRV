// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Audio;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.Sound;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.Xrv.Core.Extensions;
using System;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// Displays a marker when a QR code is detected.
    /// </summary>
    public class QRMarker : Component
    {
        [BindComponent(source: BindComponentSource.Children, tag: "PART_qrmarker_border")]
        private MaterialComponent borderMaterialComponent = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_qrmarker_icon")]
        private MaterialComponent iconMaterialComponent = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_qrmarker_icon")]
        private Transform3D iconTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_qrmarker_background")]
        private MaterialComponent backgroundMaterialComponent = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_qrmarker_background")]
        private Transform3D backgroundTransform = null;

        [BindComponent]
        private SoundEmitter3D soundEmitter = null;

        private bool isValidMarker;
        private IWorkAction animation;
        private Vector3 initialIconScale;

        /// <summary>
        /// Raised when animation has been completed.
        /// </summary>
        public event EventHandler AnimationCompleted;

        /// <summary>
        /// Gets or sets a value indicating whether marker should present
        /// a valid or invalid QR code status.
        /// </summary>
        public bool IsValidMarker
        {
            get => this.isValidMarker;
            set
            {
                if (this.isValidMarker != value)
                {
                    this.isValidMarker = value;
                    this.UpdateState();
                }
            }
        }

        /// <summary>
        /// Gets or sets valid status icon.
        /// </summary>
        public Material ValidIcon { get; set; }

        /// <summary>
        /// Gets or sets invalid status icon.
        /// </summary>
        public Material InvalidIcon { get; set; }

        /// <summary>
        /// Gets or sets valid status tint color.
        /// </summary>
        public Color ValidColor { get; set; }

        /// <summary>
        /// Gets or sets invalid status tint color.
        /// </summary>
        public Color InvalidColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether marker detection
        /// should emit a sound.
        /// </summary>
        public bool EmitsSound { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether marker detection
        /// should animate visual state change.
        /// </summary>
        public bool AnimateStateChange { get; set; } = true;

        /// <summary>
        /// Gets or sets valid status sound effect.
        /// </summary>
        public AudioBuffer ValidSound { get; set; }

        /// <summary>
        /// Gets or sets invalid status sound effect.
        /// </summary>
        public AudioBuffer InvalidSound { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.initialIconScale = this.iconTransform.LocalScale;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateState();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.StopSound();
            this.animation?.TryCancel();
            this.animation = null;
        }

        private void UpdateState()
        {
            if (!this.IsAttached)
            {
                return;
            }

            this.iconMaterialComponent.Material = this.isValidMarker ? this.ValidIcon : this.InvalidIcon;

            Color tintColor = this.isValidMarker ? this.ValidColor : this.InvalidColor;
            var holoGraphicBorder = new HoloGraphic(this.borderMaterialComponent.Material);
            holoGraphicBorder.Albedo = tintColor;
            var backgroundGraphicBorder = new HoloGraphic(this.backgroundMaterialComponent.Material);
            backgroundGraphicBorder.Albedo = tintColor;

            this.StopSound();
            this.soundEmitter.Audio = this.isValidMarker ? this.ValidSound : this.InvalidSound;

            if (Application.Current.IsEditor)
            {
                return;
            }

            if (this.soundEmitter.Audio != null && this.EmitsSound)
            {
                this.soundEmitter.Play();
            }

            if (this.AnimateStateChange)
            {
                this.animation?.TryCancel();
                this.animation = this.CreateAnimationInstance();
                this.animation.Run();
            }
            else
            {
                var localScale = this.backgroundTransform.LocalScale;
                localScale.X = 0;
                this.backgroundTransform.LocalScale = localScale;
            }
        }

        private void StopSound()
        {
            bool isPlaying = this.soundEmitter.PlayState == Evergine.Common.Media.PlayState.Playing;
            if (isPlaying)
            {
                this.soundEmitter.Stop();
            }
        }

        private IWorkAction CreateAnimationInstance()
        {
            var time = TimeSpan.FromSeconds(1.5);
            var ease = EaseFunction.CubicOutEase;
            var backgroundAnimation = this.CreateBackroundAnimation(time, ease);
            var iconAnimation = this.CreateIconAnimation(time, TimeSpan.FromSeconds(0.35), TimeSpan.FromSeconds(0.185f), ease);

            return new WorkActionSet(new[] { backgroundAnimation, iconAnimation })
                .WaitAll()
                .ContinueWithAction(() => this.AnimationCompleted?.Invoke(this, EventArgs.Empty));
        }

        private IWorkAction CreateBackroundAnimation(TimeSpan time, EaseFunction ease)
        {
            TimeSpan timeByTwo = TimeSpan.FromSeconds(time.TotalSeconds / 2);
            var positionAnimationFunc = (float init, float end, TimeSpan time, EaseFunction ease) =>
                new FloatAnimationWorkAction(
                this.Owner, init, end, time, ease, f =>
                {
                    var localPosition = this.backgroundTransform.LocalPosition;
                    localPosition.X = f;
                    this.backgroundTransform.LocalPosition = localPosition;
                });
            var positionAnimation = WorkActionFactory
                .CreateWorkAction(this.Owner.Scene, positionAnimationFunc.Invoke(-0.5f, 0, timeByTwo, ease))
                .ContinueWith(positionAnimationFunc.Invoke(0, 0.5f, timeByTwo, ease));

            var scaleAnimationFunc = (float init, float end, TimeSpan time, EaseFunction ease) =>
                new FloatAnimationWorkAction(
                this.Owner, init, end, time, ease, f =>
                {
                    var localScale = this.backgroundTransform.LocalScale;
                    localScale.X = f;
                    this.backgroundTransform.LocalScale = localScale;
                });
            var scaleAnimation = WorkActionFactory
                .CreateWorkAction(this.Owner.Scene, scaleAnimationFunc.Invoke(0, 1f, timeByTwo, ease))
                .ContinueWith(scaleAnimationFunc.Invoke(1f, 0f, timeByTwo, ease));

            return new WorkActionSet(new[] { positionAnimation, scaleAnimation }).WaitAll();
        }

        private IWorkAction CreateIconAnimation(TimeSpan time, TimeSpan delay, TimeSpan fadeTime, EaseFunction ease)
        {
            TimeSpan timeByTwo = TimeSpan.FromSeconds(time.TotalSeconds / 2);

            var positionAnimationFunc = (float init, float end, TimeSpan time, EaseFunction ease) =>
                new FloatAnimationWorkAction(
                this.Owner, init, end, time, ease, f =>
                {
                    var localPosition = this.iconTransform.LocalPosition;
                    localPosition.X = f;
                    this.iconTransform.LocalPosition = localPosition;
                });
            var positionAnimation = WorkActionFactory
                .CreateWorkAction(this.Owner.Scene, () => positionAnimationFunc.Invoke(-0.5f, -0.5f, TimeSpan.Zero, ease))
                .ContinueWith(positionAnimationFunc.Invoke(-1f, 0, timeByTwo, ease))
                .ContinueWith(positionAnimationFunc.Invoke(0, 1f, timeByTwo, ease));

            var scaleAnimationFunc = (float init, float end, TimeSpan time, EaseFunction ease) =>
                new FloatAnimationWorkAction(
                this.Owner, init, end, time, ease, f =>
                {
                    var localScale = this.iconTransform.LocalScale;
                    localScale.X = f;
                    localScale.Z = f;
                    this.iconTransform.LocalScale = localScale;
                });
            var scaleAnimation = WorkActionFactory
                .CreateWorkAction(this.Owner.Scene, () => scaleAnimationFunc.Invoke(this.initialIconScale.X, this.initialIconScale.X, TimeSpan.Zero, ease))
                .Delay(delay)
                .ContinueWith(scaleAnimationFunc.Invoke(this.initialIconScale.X, this.initialIconScale.X * 1.1f, fadeTime, ease))
                .ContinueWith(scaleAnimationFunc.Invoke(this.initialIconScale.X * 1.1f, this.initialIconScale.X, fadeTime, ease));

            var iconHoloGraphic = new HoloGraphic(this.iconMaterialComponent.Material);
            var iconOpacityFunc = (float init, float end, TimeSpan time, EaseFunction ease) =>
               new FloatAnimationWorkAction(
               this.Owner, init, end, time, ease, f =>
               {
                   var color = iconHoloGraphic.Albedo;
                   color.A = (byte)(255 * f);
                   iconHoloGraphic.Albedo = color;
               });

            var opacityAnimation = WorkActionFactory
                .CreateWorkAction(this.Owner.Scene, () => iconOpacityFunc.Invoke(0f, 0f, TimeSpan.Zero, ease))
                .Delay(delay)
                .ContinueWith(iconOpacityFunc.Invoke(0f, 1f, fadeTime, ease))
                .ContinueWith(iconOpacityFunc.Invoke(1f, 0f, fadeTime, ease));

            return new WorkActionSet(new[] { positionAnimation, scaleAnimation, opacityAnimation }).WaitAll();
        }
    }
}
