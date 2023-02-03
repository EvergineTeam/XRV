// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.UI.Cursors;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Animation for text buttons, where text is elevated when hovered.
    /// </summary>
    public class TextButtonCursorAnimation : CursorDetectorBase
    {
        private Vector3 contentDefaultPosition;
        private Vector3 contentHoverPosition;

        private IWorkAction animation;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_text_button_content")]
        private Transform3D contentTransform = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.contentDefaultPosition = this.contentTransform.LocalPosition;
                this.contentHoverPosition = this.contentTransform.LocalPosition - (Vector3.Forward * 0.002f);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnCursorDetected(bool isDetected)
        {
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
            this.animation = contentTransformAction;
            this.animation.Run();
        }

        private void AnimateLeave()
        {
            this.animation?.TryCancel();

            var animationDuration = TimeSpan.FromMilliseconds(300);
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

            this.animation = contentTransformAction;
            this.animation.Run();
        }
    }
}
