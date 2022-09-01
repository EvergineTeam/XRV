// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.MRTK.Emulation;
using System;
using System.Linq;

namespace Xrv.Core.UI.Cursors
{
    public abstract class CursorDetectorBase : Behavior
    {
        [BindComponent(isExactType: false)]
        protected Collider3D collider3D;

        private int detectedCursorIndex;

        private (Cursor cursor, Transform3D transform)[] cursors;

        public bool IsDetected => this.detectedCursorIndex >= 0;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            if (Application.Current.IsEditor)
            {
                return true;
            }

            var physicBody = this.Owner.FindComponent<PhysicBody3D>(isExactType: false);
            if (physicBody == null)
            {
                physicBody = new StaticBody3D();
                this.Owner.AddComponent(physicBody);
            }

            physicBody.IsSensor = true;
            physicBody.CollisionCategories = CollisionCategory3D.None;
            physicBody.MaskBits = CollisionCategory3D.None;

            return true;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (Application.Current.IsEditor)
            {
                return;
            }

            this.CacheCursorsIfNonePreviouslyDetected();
            this.detectedCursorIndex = -1;
            this.OnCursorDetected(false);
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            this.CacheCursorsIfNonePreviouslyDetected();

            var lastDetectedIndex = this.detectedCursorIndex;
            var lastDetected = lastDetectedIndex >= 0;
            var detected = false;
            if (lastDetected)
            {
                var item = this.cursors[this.detectedCursorIndex];
                detected = this.CheckCursorIsDetected(item.cursor, item.transform);
            }

            if (!detected)
            {
                this.detectedCursorIndex = -1;
                for (int i = 0; i < this.cursors.Length; i++)
                {
                    if (lastDetectedIndex == i)
                    {
                        continue;
                    }

                    var item = this.cursors[i];
                    if (this.CheckCursorIsDetected(item.cursor, item.transform))
                    {
                        detected = true;
                        this.detectedCursorIndex = i;
                        break;
                    }
                }
            }

            if (lastDetected != detected)
            {
                this.OnCursorDetected(detected);
            }
        }

        protected abstract void OnCursorDetected(bool isDetected);

        private bool CheckCursorIsDetected(Cursor cursor, Transform3D cursorTransform)
        {
            if (cursor.IsVisible &&
                this.collider3D.PointTest(cursorTransform.Position))
            {
                return true;
            }

            return false;
        }

        private void CacheCursorsIfNonePreviouslyDetected()
        {
            if (this.cursors?.Any() != true)
            {
                this.cursors = Cursor.ActiveCursors.Select(x => (x, x.Owner.FindComponent<Transform3D>()))
                                                   .ToArray();
            }
        }
    }
}
