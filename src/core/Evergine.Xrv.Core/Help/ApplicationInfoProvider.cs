// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
#else
using System.Reflection;
#endif

namespace Evergine.Xrv.Core.Help
{
    /// <summary>
    /// Queries data related with application.
    /// </summary>
    public class ApplicationInfoProvider
    {
        /// <summary>
        /// Gets version string from package metadata.
        /// </summary>
        /// <returns>Application version name.</returns>
        public string GetVersion()
        {
#if ANDROID
            Context ctx = Application.Context.ApplicationContext;
            PackageManager packageManager = ctx.PackageManager;
            PackageInfo info = packageManager.GetPackageInfo(ctx.PackageName, 0);
            return info.VersionName;
#else
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
        }
    }
}
