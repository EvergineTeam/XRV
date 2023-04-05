// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.Xrv.Core.Services.QR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// World center provider that scans a real world QR marker.
    /// </summary>
    public class QrWorldCenterProvider : IWorldCenterProvider
    {
        private readonly QRScanningFlow scanningFlow;
        private readonly AssetsService assetsService;

        private Entity marker;
        private Matrix4x4? pose;

        /// <summary>
        /// Initializes a new instance of the <see cref="QrWorldCenterProvider"/> class.
        /// </summary>
        /// <param name="scanningFlow">QR scanning flow.</param>
        /// <param name="assetsService">Assets service.</param>
        public QrWorldCenterProvider(
            QRScanningFlow scanningFlow,
            AssetsService assetsService)
        {
            this.scanningFlow = scanningFlow;
            if (this.scanningFlow != null)
            {
                this.scanningFlow.Completed += this.ScanningFlow_Completed;
            }

            this.assetsService = assetsService;
        }

        /// <inheritdoc/>
        public async Task<Matrix4x4?> GetWorldCenterPoseAsync(CancellationToken cancellationToken = default)
        {
            this.scanningFlow.HideMarkerAutomatically = true;

            var result = await this.scanningFlow.ExecuteFlowAsync(cancellationToken).ConfigureAwait(false);
            this.pose = result?.Pose;
            return this.pose;
        }

        /// <inheritdoc/>
        public void OnWorldCenterPoseUpdate(Entity worldCenter)
        {
            this.EnsureMarkerIsCreated(worldCenter);
        }

        /// <inheritdoc/>
        public void CleanWorldCenterEntity(Entity worldCenter)
        {
            this.marker.IsEnabled = false;
        }

        private void EnsureMarkerIsCreated(Entity worldCenter)
        {
            if (this.marker != null)
            {
                return;
            }

            // QR scanning flow is for general purpouse, and creats its own marker.
            // What we do here is to have a separated marker entity that will
            // be placed in space while networking session is running.
            var factory = new QRMarkerFactory(this.assetsService);
            this.marker = factory.CreateMarkerEntityInstance("WorldCenterMarker");

            var markerComponent = this.marker.FindComponentInChildren<QRMarker>();
            markerComponent.IsValidMarker = true;
            markerComponent.EmitsSound = false;
            markerComponent.AnimateStateChange = false;

            this.marker.IsEnabled = false;
            worldCenter.AddChild(this.marker);

            // Apply platform QR representation position fix-up
            var markerLocalTransform = markerComponent.Owner.FindComponent<Transform3D>();
            var markerLocalPosition = markerLocalTransform.LocalPosition;
            QRPlatformHelper.FixUpCodeOrigin(ref markerLocalPosition);
            markerLocalTransform.LocalPosition = markerLocalPosition;
            markerLocalTransform.LocalScale = this.pose.Value.Scale; // Respect real-world scale
        }

        private void ScanningFlow_Completed(object sender, EventArgs e)
        {
            if (this.marker != null)
            {
                this.marker.IsEnabled = this.pose.HasValue;
            }
        }
    }
}
