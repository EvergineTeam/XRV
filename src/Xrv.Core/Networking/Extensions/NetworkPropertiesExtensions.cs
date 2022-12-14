// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Components;
using System;

namespace Xrv.Core.Networking.Extensions
{
    /// <summary>
    /// Extension methods for network properties.
    /// </summary>
    public static class NetworkPropertiesExtensions
    {
        /// <summary>
        /// Resets key assigation.
        /// </summary>
        /// <typeparam name="K">The type of the property key.</typeparam>
        /// <typeparam name="V">The type of the property value.</typeparam>
        /// <param name="property">Network property.</param>
        public static void ResetKey<K, V>(this NetworkPropertySync<K, V> property)
            where K : struct, IConvertible
        {
            property.PropertyKeyByte = default;
        }

        /// <summary>
        /// Checks if key has been initialized.
        /// </summary>
        /// <typeparam name="K">The type of the property key.</typeparam>
        /// <typeparam name="V">The type of the property value.</typeparam>
        /// <param name="property">Network property.</param>
        /// <returns>True if key is initialized; false otherwise.</returns>
        public static bool HasInitializedKey<K, V>(this NetworkPropertySync<K, V> property)
            where K : struct, IConvertible
        {
            return property.PropertyKeyByte != default;
        }
    }
}
