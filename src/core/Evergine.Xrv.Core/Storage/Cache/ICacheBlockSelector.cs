// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.Storage.Cache
{
    /// <summary>
    /// Block selector for cache. For each cache file, this is used to evaluate which cache
    /// block it belongs.
    /// </summary>
    public interface ICacheBlockSelector
    {
        /// <summary>
        /// Gets cache key that a file belongs to.
        /// </summary>
        /// <param name="relativePath">File path.</param>
        /// <returns>Cache key.</returns>
        string GetKey(string relativePath);
    }
}
