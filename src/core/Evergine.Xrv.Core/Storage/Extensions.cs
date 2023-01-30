// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.IO;
using System.Linq;

namespace Evergine.Xrv.Core.Storage
{
    internal static class Extensions
    {
        public static string GetFileName(this string path)
        {
            return Path.GetFileName(path).FixSlashes();
        }

        public static string GetDirectoryName(this string path)
        {
            var directoryName = path
                .Split(new[] { DirectoryItem.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? path;

            return directoryName.FixSlashes();
        }

        public static string FixSlashes(this string path) => path.Replace(@"\", DirectoryItem.PathSeparator);
    }
}
