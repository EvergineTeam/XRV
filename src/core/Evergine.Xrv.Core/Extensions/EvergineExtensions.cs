// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Framework;
using Evergine.Framework.Services;
using System;
using System.Linq;

namespace Evergine.Xrv.Core.Extensions
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

        /// <summary>
        /// Checks if a component has already been added to an entity. If
        /// not, it adds it to the entity.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="entity">Target entity.</param>
        /// <param name="component">Component instance.</param>
        /// <param name="isExactType">Whether to match the exact type.</param>
        /// <param name="tag">Filter entities by this tag.</param>
        public static void AddComponentIfNotExists<TComponent>(
            this Entity entity,
            TComponent component,
            bool isExactType = true,
            string tag = null)
             where TComponent : Component
        {
            if (entity.FindComponent<TComponent>(isExactType, tag) == null)
            {
                entity.AddComponent(component);
            }
        }
    }
}
