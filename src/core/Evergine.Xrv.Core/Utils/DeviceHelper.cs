// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Xrv.Core.Networking.Participants;
using System;
using System.IO;
using System.Reflection;

namespace Evergine.Xrv.Core.Utils
{
    /// <summary>
    /// Some helpers for devices.
    /// </summary>
    public static class DeviceHelper
    {
        private static DeviceInfoImplementation deviceInfo;

        static DeviceHelper()
        {
            deviceInfo = new DeviceInfoImplementation();
        }

        /// <summary>
        /// Gets platform type.
        /// </summary>
        public static PlatformType PlatformType { get => deviceInfo.PlatformType; }

        /// <summary>
        /// Gets platform application folder path.
        /// </summary>
        /// <returns>Application folder path.</returns>
        public static string GetLocalApplicationFolderPath()
        {
            if (PlatformType == Evergine.Common.PlatformType.Windows
                &&
                Assembly.GetEntryAssembly() is Assembly entryAssembly)
            {
                return Path.GetDirectoryName(entryAssembly.Location);
            }
            else
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
        }
    }
}
