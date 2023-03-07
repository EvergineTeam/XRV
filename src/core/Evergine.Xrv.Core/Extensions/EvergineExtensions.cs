// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.MRTK.Effects;
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
        public static void RemoveAllChildren(this Entity entity) =>
            RemoveAllChildren(entity, _ => true);

        /// <summary>
        /// Removes all children within an entity after evaluating a condition.
        /// Items that do not satisfy the condition will not be removed.
        /// </summary>
        /// <param name="entity">Target entity.</param>
        /// <param name="evaluate">Removal evaluation function.</param>
        public static void RemoveAllChildren(this Entity entity, Func<Entity, bool> evaluate)
        {
            /*
             * Approach is leaving all non-removable items at the beginning of the
             * collection.
             */
            int currentSkipIndex = -1;

            while (entity.NumChildren > currentSkipIndex + 1)
            {
                var currentEntity = entity.ChildEntities.ElementAt(currentSkipIndex + 1);
                if (evaluate(currentEntity))
                {
                    entity.RemoveChild(currentEntity);
                }
                else
                {
                    currentSkipIndex++;
                }
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

        /// <summary>
        /// Tries to cancel a <see cref="IWorkAction"/>. It checks that action is already running
        /// before canceling it.
        /// </summary>
        /// <param name="action">Target work action.</param>
        /// <returns>True if it has been canceled; false otherwise.</returns>
        public static bool TryCancel(this IWorkAction action)
        {
            if (action?.State == WorkActionState.Running)
            {
                action.Cancel();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates holographic albedo by a given color on a material.
        /// </summary>
        /// <param name="assetsService">Assets service.</param>
        /// <param name="materialId">Material identifier.</param>
        /// <param name="color">New albedo color.</param>
        public static void UpdateHoloGraphicAlbedo(this AssetsService assetsService, Guid materialId, Color color)
        {
            var material = assetsService.Load<Material>(materialId);
            var holoGraphic = new HoloGraphic(material);
            holoGraphic.Albedo = color;
        }
    }
}
