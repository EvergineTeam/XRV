// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

#if ANDROID
using Android.OS;
#endif
using Evergine.Networking.Components;
#if UWP
using Evergine.Xrv.Core.Utils;
#endif

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

            var implementation = new DeviceInfoImplementation();
            var deviceInfo = DeviceInfo.From(implementation);
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
