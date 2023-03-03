// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using Evergine.Components.Fonts;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.UI.Cursors;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Modifies buttons to meet XRV look and feel:
    /// - Text is not displayed when button is idle.
    /// - Text is shown when user hovers the button.
    /// </summary>
    public class XrvPressableButtonLookAndFeel : CursorDetectorBase
    {
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Text", isRequired: false)]
        private Text3DMesh textMesh = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Text", isRequired: false)]
        private Transform3D textTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Icon", isRequired: false)]
        private Transform3D iconTransform = null;

        private IWorkAction animation;
        private Vector3 textDefaultPosition;
        private Vector3 textHoverPosition;
        private Vector3 iconDefaultPosition;
        private Vector3 iconHoverPosition;

        /// <summary>
        /// Gets or sets offset for the text.
        /// </summary>
        public float TextPositionOffset { get; set; } = 0f;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                var textPosition = this.textTransform.LocalPosition;
                textPosition.Y += this.TextPositionOffset;
                this.textTransform.LocalPosition = textPosition;

                this.textDefaultPosition = this.textTransform.LocalPosition;
                this.textHoverPosition = this.textDefaultPosition - (Vector3.Forward * 0.01f);
                this.iconDefaultPosition = this.iconTransform.LocalPosition;
                this.iconHoverPosition = this.iconDefaultPosition - (Vector3.Forward * 0.01f);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void SetTargetCollider()
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            if (this.Owner.FindComponent<Collider3D>(isExactType: false) == null)
            {
                this.collider3D = new BoxCollider3D()
                {
                    Size = new Vector3(ButtonConstants.SquareButtonSize),
                    Offset = new Vector3(0f, 0f, 0.02f),
                };
                this.Owner.AddComponent(this.collider3D);
            }
        }

        /// <inheritdoc/>
        protected override void OnCursorDetected(bool isDetected)
        {
            if (Application.Current.IsEditor)
            {
                return;
            }

            if (isDetected)
            {
                this.AnimateHover();
            }
            else
            {
                this.AnimateLeave();
            }
        }

        private void AnimateHover()
        {
            this.animation?.TryCancel();

            var animationDuration = TimeSpan.FromMilliseconds(300);
            var textTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.textDefaultPosition,
                this.textHoverPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value => AnimationPositionUpdate(@value, this.textTransform));
            var iconTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.iconDefaultPosition,
                this.iconHoverPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value => AnimationPositionUpdate(@value, this.iconTransform));
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
                textTransformAction,
                iconTransformAction,
                textColorAnimation)
                .WaitAll();
            this.animation.Run();
        }

        private void AnimateLeave()
        {
            this.animation?.TryCancel();

            var animationDuration = TimeSpan.FromMilliseconds(300);
            var textTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.textTransform.LocalPosition,
                this.textDefaultPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value => AnimationPositionUpdate(@value, this.textTransform));
            var iconTransformAction = new Vector3AnimationWorkAction(
                this.Owner,
                this.iconTransform.LocalPosition,
                this.iconDefaultPosition,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value => AnimationPositionUpdate(@value, this.iconTransform));
            var textColorAnimation = new FloatAnimationWorkAction(
                this.Owner,
                this.textMesh.Color.A,
                0,
                animationDuration,
                EaseFunction.CubicOutEase,
                @value =>
                {
                    var color = this.textMesh.Color;
                    color.A = (byte)@value;
                    this.textMesh.Color = color;
                });

            this.animation = this.Managers.Scene.CreateParallelWorkActions(
                textTransformAction,
                iconTransformAction,
                textColorAnimation)
                .WaitAll();
            this.animation.Run();
        }

        private static void AnimationPositionUpdate(Vector3 vector, Transform3D targetTransform)
        {
            var position = targetTransform.LocalPosition;
            position.X = vector.X;
            position.Y = vector.Y;
            position.Z = vector.Z;
            targetTransform.LocalPosition = position;
        }
    }
}
