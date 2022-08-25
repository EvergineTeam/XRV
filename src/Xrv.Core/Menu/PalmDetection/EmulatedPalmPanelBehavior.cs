// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.MRTK.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xrv.Core.Menu
{
    public class EmulatedPalmPanelBehavior : Behavior, IPalmPanelBehavior
    {
        private class CursorInfo
        {
            public Transform3D Transform;

            public MouseControlBehavior MouseControlBehavior;

            public XRHandedness Handedness;

            public bool IsPalmUp;
        }

        private bool isPalmUp;
        private float accumulatedTime;
        private List<CursorInfo> emulatedCursorInfos;

        private bool isActiveCursorDirty;

        private CursorInfo activeCursor;

        [BindService]
        protected GraphicsPresenter graphicsPresenter;

        [BindComponent]
        protected Transform3D transform;

        [RenderProperty(Tooltip = "Distance from the hand to the entity")]
        public float DistanceFromHand { get; set; } = 0.2f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking to the camera to consider it as up", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraUpperThreshold { get; set; } = 0.8f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the palm has to be looking away from the camera to consider it as down", MinLimit = 0, MaxLimit = 1)]
        public float LookAtCameraLowerThreshold { get; set; } = 0.7f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be open to consider the palm as up", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmUpperThreshold { get; set; } = 0.8f;

        [RenderPropertyAsFInput(Tooltip = "Amount that represents how much the hand has to be closed to consider the palm as down", MinLimit = 0, MaxLimit = 1)]
        public float OpenPalmLowerThreshold { get; set; } = 0.7f;

        [RenderProperty(Tooltip = "Minimum amount of time in seconds between two consecutive activation changes")]
        public float TimeBetweenActivationChanges { get; set; } = 0.5f;

        [RenderProperty(Tooltip = "The desired handedness to consider by the component. Set to Undefined to consider both hands.")]
        public XRHandedness Handedness { get; set; } = XRHandedness.Undefined;

        public XRHandedness ActiveHandedness => this.activeCursor?.Handedness ?? XRHandedness.Undefined;

        public bool IsPalmUp => this.isPalmUp;

        public Keys ToggleHandKey { get; set; } = Keys.M;

        public event EventHandler<bool> PalmUpChanged;

#pragma warning disable CS0067
        public event EventHandler<XRHandedness> ActiveHandednessChanged;
#pragma warning restore CS0067

        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.emulatedCursorInfos = new List<CursorInfo>();
            var mouseCursors = this.Managers.EntityManager.FindComponentsOfType<MouseControlBehavior>();
            foreach (var item in mouseCursors)
            {
                var cursorEntity = item.Owner;
                this.emulatedCursorInfos.Add(new CursorInfo()
                {
                    Handedness = cursorEntity.Name.Contains($"{XRHandedness.LeftHand}") ? XRHandedness.LeftHand : XRHandedness.RightHand,
                    MouseControlBehavior = item,
                    Transform = cursorEntity.FindComponent<Transform3D>(),
                });
            }

            return true;
        }

        private void ReadKeys()
        {
            var keyboardDispatcher = this.graphicsPresenter.FocusedDisplay.KeyboardDispatcher;
            foreach (var cursor in this.emulatedCursorInfos)
            {
                var cursorkey = cursor.MouseControlBehavior.Key;
                if (keyboardDispatcher.IsKeyDown(cursorkey) &&
                    keyboardDispatcher.ReadKeyState(this.ToggleHandKey) == ButtonState.Pressing)
                {
                    this.isActiveCursorDirty = true;
                    cursor.IsPalmUp = !cursor.IsPalmUp;
                }
            }
        }

        private void RefreshActiveCursor()
        {
            if (this.isActiveCursorDirty)
            {
                this.isActiveCursorDirty = false;
                if (this.activeCursor == null ||
                    !this.activeCursor.IsPalmUp)
                {
                    this.activeCursor = this.emulatedCursorInfos.FirstOrDefault(c => c.IsPalmUp && (this.Handedness == XRHandedness.Undefined || this.Handedness == c.Handedness));
                }
            }
        }

        protected override void Update(TimeSpan gameTime)
        {
            this.accumulatedTime += (float)gameTime.TotalSeconds;

            this.ReadKeys();

            // Get camera transform
            var cameraTransform = this.Managers.RenderManager?.ActiveCamera3D?.Transform;
            if (cameraTransform == null)
            {
                this.SetPalmUp(false);
            }

            if (this.accumulatedTime > this.TimeBetweenActivationChanges)
            {
                this.RefreshActiveCursor();
                this.SetPalmUp(this.activeCursor != null);
            }

            if (this.activeCursor == null)
            {
                return;
            }

            // Calculate position offset
            var offset = this.activeCursor.Handedness == XRHandedness.LeftHand ? cameraTransform.WorldTransform.Right : cameraTransform.WorldTransform.Left;

            // Flatten offset vector
            offset.Y = 0;
            offset.Normalize();

            // Update positions
            var cameraPosition = cameraTransform.Position;

            var desiredPosition = this.activeCursor.Transform.Position + (offset * this.DistanceFromHand);
            var desiredDirection = cameraPosition - desiredPosition;
            desiredDirection.Normalize();

            this.transform.Position = desiredPosition;
            this.transform.LookAt(desiredPosition + desiredDirection);
        }

        private void SetPalmUp(bool value)
        {
            if (this.isPalmUp != value)
            {
                this.isPalmUp = value;
                this.PalmUpChanged?.Invoke(this, value);

                this.accumulatedTime = 0;
            }
        }
    }
}
