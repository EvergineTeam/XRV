// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Components;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xrv.Core.Networking.Messaging;
using Xrv.Core.Networking.Properties.KeyRequest.Messages;

namespace Xrv.Core.Networking.Properties.KeyRequest
{
    /// <summary>
    /// Protocol to request networking property keys. Server will keep track
    /// of used keys and provide free ones when requested.
    /// </summary>
    public class KeyRequestProtocol : NetworkingProtocol
    {
        internal const string ProtocolName = "KeyRequest";
        private readonly NetworkSystem networking;
        private readonly IKeyStore keyStore;
        private TaskCompletionSource<byte[]> protocolCompletionTcs;

        internal KeyRequestProtocol(NetworkSystem networking)
            : this(networking, networking.KeyStore)
        {
        }

        internal KeyRequestProtocol(NetworkSystem networking, IKeyStore keyStore)
            : base(networking)
        {
            this.networking = networking;
            this.keyStore = keyStore ?? networking.KeyStore;
        }

        /// <inheritdoc/>
        public override string Name => ProtocolName;

        /// <summary>
        /// Gets assigned keys on protocol response.
        /// </summary>
        public byte[] AssignedKeys { get; private set; }

        /// <summary>
        /// Gets or sets timeout time.
        /// </summary>
        public TimeSpan ProtocolTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Request a single key to the server.
        /// </summary>
        /// <param name="provider">Provider filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Assigned networking property keys.</returns>
        public async Task<byte> RequestSingleKeyAsync(NetworkPropertyProviderFilter provider, CancellationToken cancellationToken = default)
        {
            var keys = await this.RequestSetOfKeysAsync(1, provider, cancellationToken).ConfigureAwait(false);
            return keys.First();
        }

        /// <summary>
        /// Request a set of keys to the server.
        /// </summary>
        /// <param name="numberOfKeys">Number of requested keys.</param>
        /// <param name="provider">Provider filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Assigned networking property keys.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a number of keys lower than 1 is requested.</exception>
        public async Task<byte[]> RequestSetOfKeysAsync(byte numberOfKeys, NetworkPropertyProviderFilter provider, CancellationToken cancellationToken = default)
        {
            if (numberOfKeys < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfKeys), "Number of requested keys should be at least 1");
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Start] Requesting networking keys with correlation: {this.CorrelationId}");
            cancellationToken.Register(() => this.EndProtocol());

            byte[] keys = null;
            await this.ExecuteAsync(async () =>
            {
                keys = await this.InternalRequestSetOfKeysAsync(numberOfKeys, provider, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return keys;
        }

        /// <inheritdoc/>
        protected override INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message)
        {
            RequestKeyProtocolMessage instance;
            var messageTypeAsEnum = (RequestKeyMessageType)message.Type;
            switch (messageTypeAsEnum)
            {
                case RequestKeyMessageType.ClientRequestKeys:
                    instance = new RequestNumberOfKeysMessage();
                    break;
                case RequestKeyMessageType.ServerAcceptKeysRequest:
                    instance = new AssignedKeysMessage();
                    break;
                default:
                    instance = new RequestKeyProtocolMessage();
                    break;
            }

            instance.Type = messageTypeAsEnum;
            message.To(instance);

            return instance;
        }

        /// <inheritdoc/>
        protected override void OnMessageReceivedAsServer(INetworkingMessageConverter message, int senderId)
        {
            // Ignore messages if current device is not session host (server)
            if (this.networking.Session?.CurrentUserIsHost != true)
            {
                return;
            }

            this.OnMessageReceivedImpl(message, senderId);
        }

        /// <inheritdoc/>
        protected override void OnMessageReceivedAsClient(INetworkingMessageConverter message, int senderId) =>
            this.OnMessageReceivedImpl(message, senderId);

        private void HandleKeysRequest(RequestNumberOfKeysMessage numberOfKeysRequest, int senderId)
        {
            bool succeeded = false;

            try
            {
                var registeredKeys = this.keyStore.ReserveKeys(
                    numberOfKeysRequest.NumberOfKeys,
                    this.CorrelationId,
                    senderId,
                    numberOfKeysRequest.ProviderType);
                this.AssignedKeys = registeredKeys.Select(key => key.Key).ToArray();
                succeeded = true;
            }
            catch (FullKeyStoreException fksException)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Exception reserving session keys: {fksException}: {this.CorrelationId}");
            }

            RequestKeyProtocolMessage response;
            if (succeeded)
            {
                response = new AssignedKeysMessage
                {
                    Keys = this.AssignedKeys,
                };
            }
            else
            {
                response = new RequestKeyProtocolMessage()
                {
                    Type = RequestKeyMessageType.ServerRejectsKeysRequest,
                };
            }

            this.ClientServer.SendProtocolMessageToClient(this, response, senderId, true);
            System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Sent {response.Type} message to client {senderId}: {this.CorrelationId}");
        }

        private void OnMessageReceivedImpl(INetworkingMessageConverter message, int senderId)
        {
            if (message is RequestNumberOfKeysMessage keysRequest)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Received a request of {keysRequest.NumberOfKeys} from sender {senderId}: {this.CorrelationId}");
                this.HandleKeysRequest(keysRequest, senderId);
            }
            else if (message is AssignedKeysMessage assignedKeysMessage)
            {
                this.AssignedKeys = assignedKeysMessage.Keys;
                System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Received {assignedKeysMessage.Type} message with {this.AssignedKeys?.Length} keys: {this.CorrelationId}");

                var confirmation = new RequestKeyProtocolMessage
                {
                    Type = RequestKeyMessageType.ClientConfirmsKeysReservation,
                };
                this.ClientServer.SendProtocolMessageToServer(this, confirmation);
                System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Sent {confirmation.Type} message to client {senderId}: {this.CorrelationId}");
            }
            else if (message is RequestKeyProtocolMessage standardMessage)
            {
                System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Received {standardMessage.Type} message: {this.CorrelationId}");

                switch (standardMessage.Type)
                {
                    case RequestKeyMessageType.ServerRejectsKeysRequest:
                        this.protocolCompletionTcs?.TrySetException(new FullKeyStoreException());
                        break;
                    case RequestKeyMessageType.ServerRejectsKeysConfirmation:
                        this.protocolCompletionTcs?.TrySetException(new KeysReservationTimeExpiredException());
                        break;
                    case RequestKeyMessageType.ClientConfirmsKeysReservation:
                        this.keyStore.ConfirmKeys(this.CorrelationId, senderId);
                        var confirmation = new RequestKeyProtocolMessage
                        {
                            Type = RequestKeyMessageType.ServerConfirmsKeysConfirmation,
                        };
                        this.ClientServer.SendProtocolMessageToClient(this, confirmation, senderId, true);
                        break;
                    case RequestKeyMessageType.ServerConfirmsKeysConfirmation:
                        this.protocolCompletionTcs?.TrySetResult(this.AssignedKeys);
                        break;
                    case RequestKeyMessageType.ClientCancelsKeysReservation:
                        this.keyStore.FreeKeys(this.CorrelationId, senderId);
                        break;
                }
            }
        }

        private async Task<byte[]> InternalRequestSetOfKeysAsync(byte numberOfKeys, NetworkPropertyProviderFilter provider, CancellationToken cancellation = default)
        {
            if (this.protocolCompletionTcs?.Task?.IsCompleted == false)
            {
                this.protocolCompletionTcs?.TrySetCanceled();
            }

            this.protocolCompletionTcs = new TaskCompletionSource<byte[]>();

            var requestMessage = new RequestNumberOfKeysMessage
            {
                Type = RequestKeyMessageType.ClientRequestKeys,
                NumberOfKeys = numberOfKeys,
                ProviderType = provider,
            };

            this.ClientServer.SendProtocolMessageToServer(this, requestMessage);
            System.Diagnostics.Debug.WriteLine($"[{nameof(KeyRequestProtocol)}][Messages] Sent {requestMessage.Type} message: {this.CorrelationId}");

            if (await Task.WhenAny(this.protocolCompletionTcs.Task, Task.Delay(this.ProtocolTimeout)).ConfigureAwait(false) != this.protocolCompletionTcs.Task)
            {
                throw new ProtocolTimeoutException();
            }

            this.protocolCompletionTcs.Task.Wait();
            return this.protocolCompletionTcs.Task.Result;
        }
    }
}
