using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using Xrv.Core.UI.Cursors;

namespace Xrv.Core.UI.Buttons
{
    public class ButtonCursorAnimation : CursorDetectorBase
    {
        private Vector3 contentDefaultPosition;
        private Vector3 contentHoverPosition;

        private IWorkAction animation;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_text_button_content")]
        protected Transform3D contentTransform;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.contentDefaultPosition = this.contentTransform.LocalPosition;
                this.contentHoverPosition = this.contentTransform.LocalPosition + Vector3.Forward * 0.002f;
            }

            return attached;
        }

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
            this.animation?.Cancel();

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
            this.animation?.Cancel();

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
