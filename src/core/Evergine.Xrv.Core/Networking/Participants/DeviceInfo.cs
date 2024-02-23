// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Networking;
using Lidgren.Network;
using System;
using System.Collections.Generic;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Shared information of local device that is sent to rest of
    /// participants.
    /// </summary>
    public class DeviceInfo : INetworkSerializable
    {
        /// <summary>
        /// Product key for <see cref="Extras"/>. Applies for Android-based
        /// devices.
        /// </summary>
        public const string ProductKey = "product";

        /// <summary>
        /// HoloLens key for <see cref="Extras"/>. Applies for UWP-based
        /// devices.
        /// </summary>
        public const string IsHoloLensKey = "isHoloLens";

        private static readonly Lazy<DeviceInfo> current = new Lazy<DeviceInfo>(() => From(new DeviceInfoImplementation()));

        /// <summary>
        /// Gets device name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets device model.
        /// </summary>
        public string Model { get; internal set; }

        /// <summary>
        /// Gets device manufacturer.
        /// </summary>
        public string Manufacturer { get; internal set; }

        /// <summary>
        /// Gets device running platform.
        /// </summary>
        public PlatformType PlatformType { get; internal set; }

        /// <summary>
        /// Gets some extra data that may vary depending on current device.
        /// </summary>
        public Dictionary<string, string> Extras { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets a value indicating whether device information is synchronized.
        /// </summary>
        public bool IsSynchronized { get => !string.IsNullOrEmpty(this.Model); }

        /// <summary>
        /// Gets host <see cref="DeviceInfo"/> data.
        /// </summary>
        public static DeviceInfo Current { get => current.Value; }

        /// <summary>
        /// Checks if device is a HoloLens.
        /// </summary>
        /// <returns>True if this is a HoloLens; false otherwise.</returns>
        public bool IsHoloLens()
        {
            if (this.PlatformType != PlatformType.UWP)
            {
                return false;
            }

            if (!this.Extras.ContainsKey(IsHoloLensKey))
            {
                return false;
            }

            bool isHoloLens = Convert.ToBoolean(this.Extras[IsHoloLensKey]);
            return isHoloLens;
        }

        /// <inheritdoc/>
        void INetworkSerializable.Write(NetBuffer buffer)
        {
            buffer.Write(this.Name);
            buffer.Write(this.Model);
            buffer.Write(this.Manufacturer);
            buffer.Write((int)this.PlatformType);
            buffer.Write(this.Extras.Count);
            foreach (var extra in this.Extras)
            {
                buffer.Write(extra.Key);
                buffer.Write(extra.Value);
            }
        }

        /// <inheritdoc/>
        void INetworkSerializable.Read(NetBuffer buffer)
        {
            this.Name = buffer.ReadString();
            this.Model = buffer.ReadString();
            this.Manufacturer = buffer.ReadString();
            this.PlatformType = (PlatformType)buffer.ReadInt32();

            int totalExtras = buffer.ReadInt32();
            this.Extras.Clear();

            for (int i = totalExtras; i > 0; i--)
            {
                string key = buffer.ReadString();
                string value = buffer.ReadString();
                this.Extras.Add(key, value);
            }
        }

        internal static DeviceInfo From(DeviceInfoImplementation implementation) =>
            new DeviceInfo
            {
                Name = implementation.Name,
                Model = implementation.Model,
                Manufacturer = implementation.Manufacturer,
                PlatformType = implementation.PlatformType,
            };
    }
}
