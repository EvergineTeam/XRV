// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Framework.XR.SpatialAnchors;
using Evergine.Mathematics;
using System;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// Updates entity <see cref="Transform3D"/> from data obtained from a
    /// <see cref="SpatialAnchor"/>.
    /// </summary>
    public class WorldAnchor : Behavior
    {
        [BindService(isRequired: false)]
        private XRPlatform xrPlatform = null;

        [BindSceneManager]
        private RenderManager renderManager = null;

        [BindComponent]
        private Transform3D transform = null;

        private SpatialAnchor currentAnchor;

        /// <summary>
        /// Gets or sets spatial anchor key.
        /// </summary>
        [IgnoreEvergine]
        [DontRenderProperty]
        public string AnchorKey { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets a value indicating whether scale produced from spatial
        /// anchor is ignored.
        /// </summary>
        public bool IgnoreScale { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether debugging is enabled for world
        /// anchor. It draws a 3D axis as reference.
        /// </summary>
        public bool EnableDebug { get; set; } = false;

        /// <summary>
        /// Saves spatial anchor, that will update owner's transform.
        /// </summary>
        /// <returns>True if saved; false otherwise.</returns>
        public bool SaveAnchor()
        {
            bool result = false;

            var spatialAnchorStore = this.xrPlatform?.SpatialAnchorStore;
            if (spatialAnchorStore != null)
            {
                var anchorTransform = this.transform.WorldTransform;
                var newAnchor = spatialAnchorStore.CreateSpatialAnchor(anchorTransform.Translation, anchorTransform.Orientation);
                if (newAnchor != null)
                {
                    this.RemoveAnchor();
                    spatialAnchorStore.StoreAnchor(this.AnchorKey, newAnchor);
                    this.currentAnchor = newAnchor;
                }
            }

            return result;
        }

        /// <summary>
        /// Removes current spatial anchor.
        /// </summary>
        public void RemoveAnchor()
        {
            var spatialAnchorStore = this.xrPlatform?.SpatialAnchorStore;
            if (spatialAnchorStore?.SavedAnchors?.ContainsKey(this.AnchorKey) == true)
            {
                spatialAnchorStore.RemoveAnchor(this.AnchorKey);
            }

            this.currentAnchor = null;
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.xrPlatform == null)
            {
                this.IsEnabled = false;
            }

            if (this.currentAnchor != null)
            {
                var worldTransform = this.currentAnchor.Transform;
                if (worldTransform.HasValue)
                {
                    var finalTransform = Matrix4x4.CreateFromTRS(
                        worldTransform.Value.Translation,
                        worldTransform.Value.Orientation,
                        this.IgnoreScale ? this.transform.Scale : worldTransform.Value.Scale);

                    this.transform.WorldTransform = finalTransform;
                }

                if (this.EnableDebug)
                {
                    const float DebugLinesSize = 0.1f;
                    var scale = this.transform.WorldInverseTransform.Scale.X * DebugLinesSize;
                    this.renderManager.LineBatch3D.DrawAxis(this.transform.WorldTransform, scale);
                }
            }
        }
    }
}
