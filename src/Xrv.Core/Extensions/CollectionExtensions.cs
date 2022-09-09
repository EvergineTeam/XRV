// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.
using System.Collections.Generic;

namespace Xrv.Core.Extensions
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
    }
}
