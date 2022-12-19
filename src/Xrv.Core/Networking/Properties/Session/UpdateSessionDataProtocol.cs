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

        public Task UpdateDataAsync(string propertyName, object propertyValue) =>
            this.ExecuteAsync(() =>
            {
                var request = new UpdateGlobalSessionDataRequestMessage
                {
                    PropertyName = propertyName,
                    PropertyValue = propertyValue,
                };

                this.ClientServer.SendProtocolMessageToServer(this, request);
                return Task.CompletedTask;
            });

        protected override INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message)
        {
            UpdateSessionDataRequestMessage instance = null;
            var messageTypeAsEnum = (UpdateSessionDataMessageType)message.Type;
            switch (messageTypeAsEnum)
            {
                case UpdateSessionDataMessageType.UpdateGlobalData:
                    instance = new UpdateGlobalSessionDataRequestMessage();
                    break;
                case UpdateSessionDataMessageType.UpdateGroupData:
                    instance = new UpdateSessionGroupDataRequestMessage();
                    break;
            }

            message.To(instance);
            return instance;
        }

        protected override void OnMessageReceived(INetworkingMessageConverter message, int senderId)
        {
            if (message is UpdateGlobalSessionDataRequestMessage globalMessage)
            {
                this.HandleGlobalMessage(globalMessage);
            }
            else if (message is UpdateSessionGroupDataRequestMessage groupMessage)
            {
                this.HandleGroupMessage(groupMessage);
            }
        }

        private void HandleGlobalMessage(UpdateGlobalSessionDataRequestMessage globalMessage)
        {
            System.Diagnostics.Debug.WriteLine($"[{nameof(UpdateSessionDataProtocol)}] Received global session data update for property {globalMessage.PropertyName}");
            this.updateManager.UpdateSession(globalMessage.PropertyName, globalMessage.PropertyValue);
        }

        private void HandleGroupMessage(UpdateSessionGroupDataRequestMessage groupMessage)
        {
            System.Diagnostics.Debug.WriteLine($"[{nameof(UpdateSessionDataProtocol)}] Received session data update for group {groupMessage.Data.GroupName}");
            this.updateManager.UpdateSession(groupMessage.Data);
        }
    }
}
