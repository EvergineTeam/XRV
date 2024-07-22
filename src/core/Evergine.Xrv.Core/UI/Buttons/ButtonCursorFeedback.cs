// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Xrv.Core.Extensions;
using System;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Feedback component for buttons in XRV. Replaces all Xrv.
    /// </summary>
    public class ButtonCursorFeedback : Component, IPressableButtonFeedback
    {
        private static TimeSpan animationDuration = TimeSpan.FromMilliseconds(300);

        private Vector3 contentDefaultPosition;
        private Vector3 contentHoverPosition;

        private IWorkAction animation;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Content")]
        private Transform3D contentTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Text", isRequired: false)]
        private Text3DMesh textMesh = null;

        /// <summary>
        /// Gets or sets content offset once button is being hovered.
        /// </summary>
        public Vector3 ContentOffset { get; set; } = -Vector3.Forward * 0.01f;

        /// <summary>
        /// Gets or sets a value indicating whether button text should be hidden once cursor is not over
        /// the button.
        /// </summary>
        public bool HideTextOnCursorLeave { get; set; } = true;

        /// <inheritdoc/>
        void IPressableButtonFeedback.Feedback(Vector3 pushVector, float pressRatio, bool pressed)
        {
        }

        /// <inheritdoc/>
        void IPressableButtonFeedback.FocusChanged(bool focus)
        {
            if (focus)
            {
                this.AnimateHover(animationDuration);
            }
            else
            {
                this.AnimateLeave(animationDuration);
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.contentDefaultPosition = this.contentTransform.LocalPosition;
                this.contentHoverPosition = this.contentTransform.LocalPosition + this.ContentOffset;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.AnimateLeave();
        }

        private void AnimateHover(TimeSpan animationDuration = default)
        {
            this.animation?.TryCancel();

            var contentTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.contentDefaultPosition,
                this.contentHoverPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value =>
                {
                    var position = this.contentTransform.LocalPosition;
                    position.X = @value.X;
                    position.Y = @value.Y;
                    position.Z = @value.Z;
                    this.contentTransform.LocalPosition = position;
                });
            var textColorAnimation = new FloatAnimationWorkAction(
                this.Owner,
                this.textMesh.Color.A,
                byte.MaxValue,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value =>
                {
                    var color = this.textMesh.Color;
                    color.A = (byte)@value;
                    this.textMesh.Color = color;
                });

            this.animation = this.Managers.Scene.CreateParallelWorkActions(
                contentTransformAction,
                textColorAnimation,
                textColorAnimation)
                .WaitAll();
            this.animation.Run();
        }

        private void AnimateLeave(TimeSpan animationDuration = default)
        {
            this.animation?.TryCancel();

            var contentTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.contentTransform.LocalPosition,
                this.contentDefaultPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value =>
                {
                    var position = this.contentTransform.LocalPosition;
                    position.X = @value.X;
                    position.Y = @value.Y;
                    position.Z = @value.Z;
                    this.contentTransform.LocalPosition = position;
                });
            var textColorAnimation = new FloatAnimationWorkAction(
                this.Owner,
                this.textMesh.Color.A,
                this.HideTextOnCursorLeave ? 0 : byte.MaxValue,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value =>
                {
                    var color = this.textMesh.Color;
                    color.A = (byte)@value;
                    this.textMesh.Color = color;
                });

            this.animation = this.Managers.Scene.CreateParallelWorkActions(
                contentTransformAction,
                textColorAnimation)
                .WaitAll();
            this.animation.Run();
        }
    }
}
