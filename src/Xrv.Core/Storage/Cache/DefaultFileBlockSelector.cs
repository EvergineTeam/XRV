// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Storage.Cache
{
    /// <summary>
    /// Considers each individual cache file as a cache block unit.
    /// </summary>
    public class DefaultFileBlockSelector : ICacheBlockSelector
    {
        /// <inheritdoc/>
        public string GetKey(string relativePath) => relativePath;
    }
}
