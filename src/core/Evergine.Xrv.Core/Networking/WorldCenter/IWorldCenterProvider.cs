// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Threading;
using System.Threading.Tasks;
using Evergine.Framework;
using Evergine.Mathematics;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// Provider to specify world center pose for networking sessions.
    /// </summary>
    public interface IWorldCenterProvider
    {
        /// <summary>
        /// Gets world center pose from a virtual or real world marker.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Marker pose.</returns>
        Task<Matrix4x4?> GetWorldCenterPoseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executed when world center pose is established.
        /// </summary>
        /// <param name="worldCenter">World center entity.</param>
        void OnWorldCenterPoseUpdate(Entity worldCenter);

        /// <summary>
        /// Point to clean any 3D element that has been added under world
        /// center entity hierarchy.
        /// </summary>
        /// <param name="worldCenter">World center entity.</param>
        void CleanWorldCenterEntity(Entity worldCenter);
    }
}
