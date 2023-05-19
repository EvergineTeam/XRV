// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.Extensions
{
    internal static class CollectionExtensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                set.Add(item);
            }
        }

        // ConcurrentQueue.Clear not supported in .netstandard2
        public static void ClearImpl<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.Any())
            {
                queue.TryDequeue(out var _);
            }
        }
    }
}
