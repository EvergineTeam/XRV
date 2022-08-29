// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;

namespace Xrv.Core.UI.Windows
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
            get => distances[NearKey];
            set => distances[NearKey] = value;
        }

        public float Medium
        {
            get => distances[MediumKey];
            set => distances[MediumKey] = value;
        }

        public float Far
        {
            get => distances[FarKey];
            set => distances[FarKey] = value;
        }

        public void SetDistance(string key, float distance)
        {
            if (distances.ContainsKey(key))
            {
                distances.Add(key, distance);
            }
            else
            {
                distances[key] = distance;
            }
        }

        public float? GetDistance(string key) =>
            distances.ContainsKey(key) ? distances[key] : (float?)null;

        public float GetDistanceOrAlternative(string key, string alternativeKey)
        {
            if (!string.IsNullOrEmpty(key) && GetDistance(key) is float distance)
            {
                return distance;
            }

            return GetDistance(alternativeKey) ?? 0;
        }

        public void RemoveDistance(string key)
        {
            if (key == NearKey || key == MediumKey || key == FarKey)
            {
                throw new InvalidOperationException($"Distance with key {key} can't be removed, as it is a default distance");
            }

            distances.Remove(key);
        }
    }
}
