// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// Factory to create virtual QR marker representation.
    /// </summary>
    public class QRMarkerFactory
    {
        private readonly AssetsService assetsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="QRMarkerFactory"/> class.
        /// </summary>
        /// <param name="assetsService">Assets service.</param>
        public QRMarkerFactory(AssetsService assetsService)
        {
            this.assetsService = assetsService;
        }

        /// <summary>
        /// Creates an instance for virtual QR marker.
        /// </summary>
        /// <param name="name">Entity name.</param>
        /// <returns>A new entity containing QR marker.</returns>
        public Entity CreateMarkerEntityInstance(string name = null)
        {
            string markerName = name ?? "qrMarker";
            var markerPrefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.Services.QR.QrMarker_weprefab);
            var qrMarkerEntity = markerPrefab.Instantiate();
            qrMarkerEntity.Name = markerName;
            var qrMarkerEntityPivot = new Entity($"{markerName}_Pivot")
                .AddComponent(new Transform3D())
                .AddChild(qrMarkerEntity);

            return qrMarkerEntityPivot;
        }
    }
}
