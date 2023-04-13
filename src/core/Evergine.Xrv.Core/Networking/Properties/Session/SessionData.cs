// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using Lidgren.Network;
using System.Collections.Generic;
using System.Text;

namespace Evergine.Xrv.Core.Networking.Properties.Session
{
    /// <summary>
    /// Networking session data.
    /// </summary>
    public class SessionData : INetworkSerializable
    {
        private Dictionary<string, SessionDataGroup> groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionData"/> class.
        /// </summary>
        public SessionData()
        {
            this.groups = new Dictionary<string, SessionDataGroup>();
        }

        /// <summary>
        /// Gets session presenter client identifier.
        /// </summary>
        public int PresenterId { get; internal set; }

        /// <summary>
        /// Tries to obtain session data for a given group.
        /// </summary>
        /// <typeparam name="TValue">Data type.</typeparam>
        /// <param name="groupName">Group name.</param>
        /// <param name="value">Group data.</param>
        /// <returns>True if group data was found; false otherwise.</returns>
        public bool TryGetGroupData<TValue>(string groupName, out TValue @value)
            where TValue : class, INetworkSerializable
        {
            @value = default;

            if (this.groups.TryGetValue(groupName, out SessionDataGroup groupData))
            {
                @value = groupData.GroupData as TValue;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var builder = new StringBuilder()
                .AppendLine($"{this.groups.Count} groups")
                .AppendLine("Groups:");

            foreach (var group in this.groups)
            {
                builder = builder.AppendLine(group.ToString());
            }

            return builder.ToString();
        }

        /// <inheritdoc/>
        void INetworkSerializable.Read(NetBuffer buffer)
        {
            this.PresenterId = buffer.ReadInt32();

            this.groups.Clear();
            int numberOfGroups = buffer.ReadInt32();
            for (int i = 0; i < numberOfGroups; i++)
            {
                var groupData = new SessionDataGroup();
                groupData.Read(buffer);
                this.groups.Add(groupData.GroupName, groupData);
            }
        }

        /// <inheritdoc/>
        void INetworkSerializable.Write(NetBuffer buffer)
        {
            buffer.Write(this.PresenterId);

            buffer.Write(this.groups.Count);
            foreach (var kvp in this.groups)
            {
                kvp.Value.Write(buffer);
            }
        }

        internal void SetGroupData(SessionDataGroup data)
        {
            if (!this.groups.ContainsKey(data.GroupName))
            {
                var groupData = new SessionDataGroup
                {
                    GroupName = data.GroupName,
                    GroupData = data.GroupData,
                };

                this.groups[data.GroupName] = groupData;
            }

            this.groups[data.GroupName].GroupData = data.GroupData;
        }
    }
}
