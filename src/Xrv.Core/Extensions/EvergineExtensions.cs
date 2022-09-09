// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.Linq;

namespace Xrv.Core.Extensions
{
    /// <summary>
    /// Extension methods for Evergine.
    /// </summary>
    public static class EvergineExtensions
    {
        /// <summary>
        /// Loads an asset only if provided identifier is no <see cref="Guid.Empty"/>.
        /// </summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="assetsService">Assets service.</param>
        /// <param name="assetId">Asset identifier.</param>
        /// <returns>Asset, if found.</returns>
        public static T LoadIfNotDefaultId<T>(this AssetsService assetsService, Guid assetId)
            where T : ILoadable =>
            assetId != Guid.Empty ? assetsService.Load<T>(assetId) : default(T);

        /// <summary>
        /// Removes all children within an entity.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        public static void RemoveAllChildren(this Entity entity)
        {
            while (entity.NumChildren > 0)
            {
                entity.RemoveChild(entity.ChildEntities.ElementAt(0));
            }
        }
    }
}
