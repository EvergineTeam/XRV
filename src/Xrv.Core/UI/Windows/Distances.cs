// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;

namespace Xrv.Core.UI.Windows
{
    /// <summary>
    /// Predefined instances to place 3D objects.
    /// </summary>
    public class Distances
    {
        /// <summary>
        /// Near distance key.
        /// </summary>
        public const string NearKey = nameof(NearKey);

        /// <summary>
        /// Medium distance key.
        /// </summary>
        public const string MediumKey = nameof(MediumKey);

        /// <summary>
        /// Far distance key.
        /// </summary>
        public const string FarKey = nameof(FarKey);

        private const float DefaultNear = 0.4f;
        private const float DefaultMedium = 0.6f;
        private const float DefaultFar = 1f;

        private Dictionary<string, float> distances = new Dictionary<string, float>()
        {
            { NearKey, DefaultNear },
            { MediumKey, DefaultMedium },
            { FarKey, DefaultFar },
        };

        /// <summary>
        /// Gets or sets near distance.
        /// </summary>
        public float Near
        {
            get => this.distances[NearKey];
            set => this.distances[NearKey] = value;
        }

        /// <summary>
        /// Gets or sets medium distance.
        /// </summary>
        public float Medium
        {
            get => this.distances[MediumKey];
            set => this.distances[MediumKey] = value;
        }

        /// <summary>
        /// Gets or sets far distance.
        /// </summary>
        public float Far
        {
            get => this.distances[FarKey];
            set => this.distances[FarKey] = value;
        }

        /// <summary>
        /// Adds or updates a predefined distance.
        /// </summary>
        /// <param name="key">Distance key.</param>
        /// <param name="distance">Distance value.</param>
        public void SetDistance(string key, float distance)
        {
            if (this.distances.ContainsKey(key))
            {
                this.distances.Add(key, distance);
            }
            else
            {
                this.distances[key] = distance;
            }
        }

        /// <summary>
        /// Gets a distance by its key.
        /// </summary>
        /// <param name="key">Distance key.</param>
        /// <returns>Distance value, if key is found; null otherwise.</returns>
        public float? GetDistance(string key) =>
            this.distances.ContainsKey(key) ? this.distances[key] : (float?)null;

        /// <summary>
        /// Gets a distance by its key. If distance key is not registered, tries with an
        /// alternative key.
        /// </summary>
        /// <param name="key">Distance key.</param>
        /// <param name="alternativeKey">Alternative distance key.</param>
        /// <returns>Distance value.</returns>
        public float GetDistanceOrAlternative(string key, string alternativeKey)
        {
            if (!string.IsNullOrEmpty(key) && this.GetDistance(key) is float distance)
            {
                return distance;
            }

            return this.GetDistance(alternativeKey) ?? 0;
        }

        /// <summary>
        /// Deletes a predefined distance. Default distances can't be deleted.
        /// </summary>
        /// <param name="key">Distance key.</param>
        /// <exception cref="InvalidOperationException">Raised when trying to delete a predefined distance.</exception>
        public void DeleteDistance(string key)
        {
            if (key == NearKey || key == MediumKey || key == FarKey)
            {
                throw new InvalidOperationException($"Distance with key {key} can't be removed, as it is a default distance");
            }

            this.distances.Remove(key);
        }
    }
}
