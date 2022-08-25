using Evergine.Components.Fonts;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using System;
using Xrv.Core.UI.Cursors;

namespace Xrv.Core.UI.Buttons
{
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

        public float TextPositionOffset { get; set; } = 0f;

        public static XrvPressableButtonLookAndFeel ApplyTo(Entity button)
        {
            if (button.FindComponent<Collider3D>(isExactType: false) == null)
            {
                button.AddComponent(new BoxCollider3D()
                {
                    Size = new Vector3(ButtonConstants.SquareButtonSize),
                    Offset = new Vector3(0f, 0f, -0.01f),
                });
            }

            var component = new XrvPressableButtonLookAndFeel();
            button.AddComponent(component);

            return component;
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.textMesh.ScaleFactor = 0.004f;

                var textPosition = this.textTransform.LocalPosition;
                textPosition.Y += this.TextPositionOffset;
                this.textTransform.LocalPosition = textPosition;

                this.textDefaultPosition = this.textTransform.LocalPosition;
                this.textHoverPosition = this.textDefaultPosition + Vector3.Forward * 0.01f;
                this.iconDefaultPosition = this.iconTransform.LocalPosition;
                this.iconHoverPosition = this.iconDefaultPosition + Vector3.Forward * 0.01f;
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
            this.animation?.Cancel();

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
