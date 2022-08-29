// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;

namespace Xrv.Core.UI
{
    public class Distances
    {
        public const string NearKey = nameof(NearKey);
        public const string MediumKey = nameof(MediumKey);
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

        public float Near
        {
            get => this.distances[NearKey];
            set => this.distances[NearKey] = value;
        }

        public float Medium
        {
            get => this.distances[MediumKey];
            set => this.distances[MediumKey] = value;
        }

        public float Far
        {
            get => this.distances[FarKey];
            set => this.distances[FarKey] = value;
        }

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

        public float? GetDistance(string key) =>
            this.distances.ContainsKey(key) ? this.distances[key] : (float?)null;

        public float GetDistanceOrAlternative(string key, string alternativeKey)
        {
            if (!string.IsNullOrEmpty(key) && this.GetDistance(key) is float distance)
            {
                return distance;
            }

            return this.GetDistance(alternativeKey) ?? 0;
        }

        public void RemoveDistance(string key)
        {
            if (key == NearKey || key == MediumKey || key == FarKey)
            {
                throw new InvalidOperationException($"Distance with key {key} can't be removed, as it is a default distance");
            }

            this.distances.Remove(key);
        }
    }
}
