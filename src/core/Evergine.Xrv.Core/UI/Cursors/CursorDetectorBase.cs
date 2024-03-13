// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.MRTK.Emulation;
using System;
using System.Linq;

namespace Evergine.Xrv.Core.UI.Cursors
{
    /// <summary>
    /// Detects cursor hover and element.
    /// </summary>
    public abstract class CursorDetectorBase : Behavior
    {
        /// <summary>
        /// Detection collider.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, isExactType: false)]
        protected Collider3D collider3D = null;

        private int detectedCursorIndex;

        private (Cursor cursor, Transform3D transform)[] cursors;

        /// <summary>
        /// Gets a value indicating whether hover is detected.
        /// </summary>
        public bool IsDetected => this.detectedCursorIndex >= 0;

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (Application.Current.IsEditor)
            {
                return;
            }

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
            if (lastDetected && this.cursors.Length > this.detectedCursorIndex)
            {
                var item = this.cursors[this.detectedCursorIndex];
                detected = this.CheckCursorIsDetected(item.cursor, item.transform);
            }

            if (!detected && this.collider3D?.IsEnabled == true)
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

        /// <summary>
        /// Invoked when cursor hover starts/stops being detected.
        /// </summary>
        /// <param name="isDetected">True if detected; false if not.</param>
        protected abstract void OnCursorDetected(bool isDetected);

        private bool CheckCursorIsDetected(Cursor cursor, Transform3D cursorTransform)
        {
            if (cursor.IsVisible && (this.collider3D?.PointTest(cursorTransform.Position) ?? false))
            {
                return true;
            }

            return false;
        }

        private void CacheCursorsIfNonePreviouslyDetected()
        {
            if (this.cursors == null || this.cursors.Length != Cursor.ActiveCursors.Count())
            {
                this.cursors = Cursor.ActiveCursors.Select(x => (x, x.Owner.FindComponent<Transform3D>()))
                                                   .ToArray();
            }
        }
    }
}
