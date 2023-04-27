// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework.XR;
using Evergine.Mathematics;
using Evergine.Networking;
using Evergine.Xrv.Core.Networking.Extensions;
using Lidgren.Network;

namespace Evergine.Xrv.Core.Networking.Participants
{
    internal class XRDeviceInfo : INetworkSerializable
    {
        public XRHandedness Handedness { get; set; }

        public Matrix4x4? Pose { get; internal set; }

        public void Write(NetBuffer buffer)
        {
            buffer.Write((byte)this.Handedness);
            buffer.WriteNullableMatrix4x4(this.Pose);
        }

        public void Read(NetBuffer buffer)
        {
            this.Handedness = (XRHandedness)buffer.ReadByte();
            this.Pose = buffer.ReadNullableMatrix4x4();
        }
    }
}
