// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Lidgren.Network;
using System;
using System.Text;

namespace Evergine.Xrv.Core.Networking.Properties.Session
{
    internal class SessionDataGroup : INetworkSerializable
    {
        public string GroupName { get; set; }

        public INetworkSerializable GroupData { get; set; }

        public void Read(NetBuffer buffer)
        {
            this.GroupName = buffer.ReadString();
            var groupDataTypeName = buffer.ReadString();

            var type = Type.GetType(groupDataTypeName);
            if (!typeof(INetworkSerializable).IsAssignableFrom(type))
            {
                throw new InvalidCastException($"Type {groupDataTypeName} must implement {nameof(INetworkSerializable)}");
            }

            var groupData = (INetworkSerializable)Activator.CreateInstance(type);
            groupData.Read(buffer);
            this.GroupData = groupData;
        }

        public void Write(NetBuffer buffer)
        {
            buffer.Write(this.GroupName);
            buffer.Write(this.GroupData.GetType().AssemblyQualifiedName);
            this.GroupData.Write(buffer);
        }

        public override string ToString()
        {
            var builder = new StringBuilder()
                .AppendLine($"- Name: {this.GroupName}")
                .AppendLine($"- Data: ")
                .AppendLine(this.GroupData.ToString());

            return builder.ToString();
        }
    }
}
