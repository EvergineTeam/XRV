// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

#if UWP
using Windows.ApplicationModel;
#elif ANDROID
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
#if UWP
            PackageVersion version = Package.Current.Id.Version;
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
#elif NET8_0_ANDROID
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
