// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Platform;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Xrv.Core.Utils
{
    /// <summary>
    /// Some helpers for devices.
    /// </summary>
    public static class DeviceHelper
    {
        /// <summary>
        /// Gets platform application folder path.
        /// </summary>
        /// <returns>Application folder path.</returns>
        public static string GetLocalApplicationFolderPath()
        {
            if (DeviceInfo.PlatformType == Evergine.Common.PlatformType.Windows
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

        /// <summary>
        /// Evaluates if current device is a HoloLens.
        /// </summary>
        /// <returns>True if current device is HoloLens; false otherwise.</returns>
        public static bool IsHoloLens()
        {
            bool isHoloLens = false;

#if UWP
            isHoloLens = Windows.ApplicationModel.Preview.Holographic.HolographicApplicationPreview.IsCurrentViewPresentedOnHolographicDisplay();
#endif

            return isHoloLens;
        }

        /// <summary>
        /// Ensures camera permissions have been granted.
        /// </summary>
        /// <returns>True if granted; false otherwise.</returns>
        public static async Task<bool> EnsureCameraPersmissionAsync()
        {
#if UWP
            const int NoCaptureDevicesHResult = -1072845856; // ‭0xC00DABE0
            using (var mediaCapture = new Windows.Media.Capture.MediaCapture())
            {
                try
                {
                    await mediaCapture.InitializeAsync(new Windows.Media.Capture.MediaCaptureInitializationSettings
                    {
                        StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.AudioAndVideo,
                    });

                    return true;
                }
                catch (Exception ex) when (ex.HResult == NoCaptureDevicesHResult)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }
#else
            return await Task.FromResult(true);
#endif
        }
    }
}
