﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Threading.Tasks;
using Xrv.Core.Networking.Messaging;
using Xrv.Core.Networking.Properties.Session;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Networking.ControlRequest
{
    internal class ControlRequestProtocol : NetworkingProtocol
    {
        internal const string ProtocolName = "ControlRequest";

        private readonly NetworkSystem network;
        private readonly WindowsSystem windows;
        private readonly SessionDataUpdateManager updateManager;

        private int? currentControlRequesterId;
        private TaskCompletionSource<bool> controlRequestTcs;

        public ControlRequestProtocol(
            NetworkSystem network,
            WindowsSystem windows,
            SessionDataUpdateManager updateManager)
            : base(network)
        {
            this.network = network;
            this.windows = windows;
            this.updateManager = updateManager;
        }

        public override string Name => ProtocolName;

        /// <summary>
        /// Gets or sets timeout time.
        /// </summary>
        public TimeSpan ProtocolTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public async Task<bool> RequestControlAsync()
        {
            bool granted = false;

            var sessionData = this.network.Session.Data;
            this.TargetClientId = sessionData.PresenterId == 0 ? null : sessionData.PresenterId;

            await this.ExecuteAsync(async () =>
            {
                granted = await this.InternalRequestControlAsync();
            });

            return granted;
        }

        public async Task TakeControlAsync()
        {
            var sessionData = this.network.Session.Data;
            var currentPresenterId = sessionData.PresenterId;
            var myClientId = this.network.Client.ClientId;

            if (currentPresenterId != myClientId)
            {
                this.TargetClientId = sessionData.PresenterId == 0 ? null : sessionData.PresenterId;

                await this.ExecuteAsync(this.InternalTakeControlAsync).ConfigureAwait(false);
                await this.UpdatePresenterAsync(this.network.Client.ClientId).ConfigureAwait(false);
            }
        }

        protected override INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message)
        {
            ControlRequestMessage instance = null;
            var messageTypeAsEnum = (ControlRequestMessageType)message.Type;
            switch (messageTypeAsEnum)
            {
                case ControlRequestMessageType.ClientRequestControl:
                case ControlRequestMessageType.ControlTaken:
                    instance = new ControlRequestMessage
                    {
                        Type = messageTypeAsEnum,
                    };
                    break;
                case ControlRequestMessageType.ControlRequestResult:
                    instance = new ControlRequestResultMessage();
                    break;
            }

            message.To(instance);
            return instance;
        }

        protected override void OnMessageReceived(INetworkingMessageConverter message, int senderId)
        {
            if (message is ControlRequestResultMessage result)
            {
                this.controlRequestTcs.TrySetResult(result.Accepted);
            }
            else if (message is ControlRequestMessage controlRequest)
            {
                if (controlRequest.Type == ControlRequestMessageType.ControlTaken)
                {
                    this.HandleControlTakenFromHost();
                }
                else
                {
                    this.HandleControlRequestFromClient(senderId);
                }
            }
        }

        private async Task<bool> InternalRequestControlAsync()
        {
            if (this.controlRequestTcs?.Task?.IsCompleted == false)
            {
                this.controlRequestTcs?.TrySetCanceled();
            }

            this.controlRequestTcs = new TaskCompletionSource<bool>();

            var session = this.network.Session;
            this.ClientServer.SendProtocolMessageToClient(this, new ControlRequestMessage(), session.Data.PresenterId);

            if (await Task.WhenAny(this.controlRequestTcs.Task, Task.Delay(this.ProtocolTimeout)).ConfigureAwait(false) != this.controlRequestTcs.Task)
            {
                throw new ProtocolTimeoutException();
            }

            this.controlRequestTcs.Task.Wait();
            return this.controlRequestTcs.Task.Result;
        }

        private Task InternalTakeControlAsync()
        {
            var session = this.network.Session;
            var message = new ControlRequestMessage
            {
                Type = ControlRequestMessageType.ControlTaken,
            };

            this.ClientServer.SendProtocolMessageToClient(this, message, session.Data.PresenterId);
            return Task.CompletedTask;
        }

        private void HandleControlRequestFromClient(int senderId)
        {
            var session = this.network.Session;
            var client = this.network.Client;

            // Check that current client is the session presenter
            if (session.Data.PresenterId != client.ClientId)
            {
                return;
            }

            // Show confirmation dialog to user, that may select an
            // option asynchronously.
            this.currentControlRequesterId = senderId;
            var dialog = this.windows.ShowConfirmDialog(
                "Control request",
                "Someone has requested to take control.\nDo you agree?",
                "No",
                "Yes");
            dialog.Closed += this.ControlRequestConfirmation_Closed;
        }

        private void ControlRequestConfirmation_Closed(object sender, EventArgs args)
        {
            if (sender is ConfirmDialog confirmDialog)
            {
                confirmDialog.Closed -= this.ControlRequestConfirmation_Closed;

                if (confirmDialog.Result == ConfirmDialog.AcceptKey)
                {
                    System.Diagnostics.Debug.WriteLine($"[{nameof(ControlRequestProtocol)}] Control request accepted, updating session data");
                    _ = this.UpdatePresenterAsync(this.currentControlRequesterId.Value)
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{nameof(ControlRequestProtocol)}] Error changing presenter: {t.Exception}");
                            }
                        });
                }

                bool acceptedRequest = confirmDialog.Result == ConfirmDialog.AcceptKey;
                System.Diagnostics.Debug.WriteLine($"[{nameof(ControlRequestProtocol)}] Sending control request response: {acceptedRequest}");

                var confirmation = new ControlRequestResultMessage
                {
                    Accepted = acceptedRequest,
                };
                this.ClientServer.SendProtocolMessageToClient(this, confirmation, this.currentControlRequesterId.Value);
            }
        }

        private Task UpdatePresenterAsync(int newPresenterId)
        {
            var updateSessionProtocol = new UpdateSessionDataProtocol(this.network, this.updateManager);
            return updateSessionProtocol.UpdateDataAsync(nameof(SessionData.PresenterId), newPresenterId);
        }

        private void HandleControlTakenFromHost() =>
            this.windows.ShowAlertDialog(
                "Control lost",
                "Session host has taken session control",
                "OK");
    }
}
