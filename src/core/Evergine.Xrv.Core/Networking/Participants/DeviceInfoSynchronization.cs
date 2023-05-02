// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

#if ANDROID
using Android.OS;
#endif
using Evergine.Networking.Components;
#if UWP
using Evergine.Xrv.Core.Utils;
#endif
using EDeviceInfo = Evergine.Platform.DeviceInfo;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class DeviceInfoSynchronization : NetworkSerializablePropertySync<byte, DeviceInfo>
    {
        public DeviceInfoSynchronization()
        {
            this.ProviderFilter = NetworkPropertyProviderFilter.Player;
            this.PropertyKey = SessionParticipants.DeviceInfoPropertyKey;
        }

        protected override void OnPropertyReadyToSet()
        {
            base.OnPropertyReadyToSet();

            var deviceInfo = new DeviceInfo
            {
                Name = EDeviceInfo.Name,
                Model = EDeviceInfo.Model,
                Manufacturer = EDeviceInfo.Manufacturer,
                PlatformType = EDeviceInfo.PlatformType,
            };
#if ANDROID
            deviceInfo.Extras.Add(DeviceInfo.ProductKey, Build.Product);
#elif UWP
            deviceInfo.Extras.Add(DeviceInfo.IsHoloLensKey, DeviceHelper.IsHoloLens().ToString());
#endif

            this.PropertyValue = deviceInfo;
        }

        protected override void OnPropertyAddedOrChanged()
        {
        }

        protected override void OnPropertyRemoved()
        {
        }
    }
}
