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
            this.marker = new Entity("WorldCenterMarker-Pivot")
                .AddComponent(new Transform3D());
            var factory = new QRMarkerFactory(this.assetsService);
            var visual = factory.CreateMarkerEntityInstance("WorldCenterMarker");

            var markerComponent = visual.FindComponentInChildren<QRMarker>();
            markerComponent.IsValidMarker = true;
            markerComponent.EmitsSound = false;
            markerComponent.AnimateStateChange = false;

            this.marker.IsEnabled = false;
            this.marker.AddChild(visual);
            worldCenter.AddChild(this.marker);

            // Apply platform QR representation position fix-up
            var visualMarkerTransform = markerComponent.Owner.FindComponent<Transform3D>();
            var markerLocalPosition = visualMarkerTransform.LocalPosition;
            QRPlatformHelper.FixUpCodeOrigin(ref markerLocalPosition);
            visualMarkerTransform.LocalPosition = markerLocalPosition;

            var pivotTransform = this.marker.FindComponent<Transform3D>();
            pivotTransform.LocalScale = this.pose.Value.Scale; // Respect real-world scale
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
