﻿// <auto-generated />
// adapted from https://github.com/dotnet/maui/blob/main/src/Essentials/src/DeviceInfo

using Evergine.Common;
using System.Runtime.InteropServices;
#if UWP
using System;
using System.Diagnostics;
using Windows.Security.ExchangeActiveSyncProvisioning;
#elif ANDROID
using System;
using Android.App;
using Android.OS;
#endif

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class DeviceInfoImplementation
    {
        public PlatformType PlatformType { get; internal set; }

#if UWP
        readonly EasClientDeviceInformation deviceInfo;
        string systemProductName;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceInfoImplementation"/> class.
        /// </summary>
        public DeviceInfoImplementation()
        {
            deviceInfo = new EasClientDeviceInformation();
            this.PlatformType = PlatformType.UWP;
            try
            {
                systemProductName = deviceInfo.SystemProductName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get system product name. {ex.Message}");
            }
        }

        public string Model => deviceInfo.SystemProductName;

        public string Manufacturer => deviceInfo.SystemManufacturer;

        public string Name => deviceInfo.FriendlyName;
#elif ANDROID
        public DeviceInfoImplementation()
        {
            PlatformType = PlatformType.Android;
        }

        public string Model => Build.Model;

	    public string Manufacturer => Build.Manufacturer;

	    public string Name
	    {
		    get
		    {
			    // DEVICE_NAME added in System.Global in API level 25
			    // https://developer.android.com/reference/android/provider/Settings.Global#DEVICE_NAME
			    var name = GetSystemSetting("device_name", true);
			    if (string.IsNullOrWhiteSpace(name))
				    name = Model;
			    return name;
		    }
	    }

	    static string GetSystemSetting(string name, bool isGlobal = false)
	    {
            if (isGlobal && OperatingSystem.IsAndroidVersionAtLeast(25))
			    return global::Android.Provider.Settings.System.GetString(Application.Context.ContentResolver, name);
		    else
			    return global::Android.Provider.Settings.System.GetString(Application.Context.ContentResolver, name);
        }
#else
        public DeviceInfoImplementation()
        {
            this.PlatformType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? PlatformType.Windows 
                : PlatformType.Undefined;
        }

        public string Model => "Unknown";

        public string Manufacturer => "Unknown";

        public string Name => "Unknown";
#endif
    }
}