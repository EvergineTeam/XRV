// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Input;
using Evergine.Common.Input.Keyboard;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.Mathematics;
using Evergine.MRTK.Behaviors;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.Menu.PalmDetection
{
    /// <summary>
    /// Emulates palm panel for platforms like Windows, where users do not use their
    /// hands to load hand menu.
    /// </summary>
    public class EmulatedPalmPanelBehavior : PalmPanelBehaviorBase
    {
        private List<CursorInfo> emulatedCursorInfos;

        private bool isActiveCursorDirty;

        private CursorInfo activeCursor;

        [BindService]
        private GraphicsPresenter graphicsPresenter = null;

        /// <inheritdoc/>
        public override XRHandedness ActiveHandedness => this.activeCursor?.Handedness ?? XRHandedness.Undefined;

        /// <summary>
        /// Gets or sets keyboard key to simulate palm twist.
        /// </summary>
        public Keys ToggleHandKey { get; set; } = Keys.M;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        protected override void Prepare()
        {
            this.ReadKeys();
        }

        /// <inheritdoc/>
        protected override bool GetNewPalmUp()
        {
            this.RefreshActiveCursor();
            return this.activeCursor != null;
        }

        /// <inheritdoc/>
        protected override Vector3? GetAnchorPoint()
        {
            return this.activeCursor?.Transform.Position;
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

        private class CursorInfo
        {
            public Transform3D Transform;

            public MouseControlBehavior MouseControlBehavior;

            public XRHandedness Handedness;

            public bool IsPalmUp;
        }
    }
}
