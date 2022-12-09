// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;
using System.Threading.Tasks;
using Xrv.Core.Networking.Messaging;

namespace Xrv.Core.Networking.Properties.Session
{
    internal class UpdateSessionDataProtocol : NetworkingProtocol
    {
        internal const string ProtocolName = "UpdateSessionData";

        private readonly SessionDataUpdateManager updateManager;

        public UpdateSessionDataProtocol(NetworkSystem networking, SessionDataUpdateManager updateManager)
            : base(networking)
        {
            this.updateManager = updateManager;
        }

        public override string Name => ProtocolName;

        public Task UpdateDataAsync(string groupName, INetworkSerializable data) =>
            this.ExecuteAsync(() =>
            {
                var request = new UpdateSessionGroupDataRequestMessage
                {
                    Data = new SessionDataGroup
                    {
                        GroupName = groupName,
                        GroupData = data,
                    },
                };

                this.ClientServer.SendProtocolMessageToServer(this, request);
                return Task.CompletedTask;
            });

        protected override INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message)
        {
            var request = new UpdateSessionGroupDataRequestMessage
            {
                Type = UpdateSessionDataMessageType.UpdateGroupData,
            };
            message.To(request);

            return request;
        }

        protected override void OnMessageReceivedAsServer(INetworkingMessageConverter message, int senderId) =>
            this.OnMessageReceivedAsClient(message, senderId);

        protected override void OnMessageReceivedAsClient(INetworkingMessageConverter message, int senderId)
        {
            var groupUpdateMessage = message as UpdateSessionGroupDataRequestMessage;
            if (groupUpdateMessage == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(UpdateSessionDataProtocol)}] Received session data update for group {groupUpdateMessage.Data.GroupName}");
            this.updateManager.UpdateSession(groupUpdateMessage.Data);
        }
    }
}
