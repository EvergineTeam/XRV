// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using System;
using System.Threading.Tasks;

namespace Xrv.Core.Networking.Messaging
{
    internal class ProtocolStarter
    {
        private readonly NetworkingProtocol protocol;
        private readonly ILifecycleMessaging lifecycle;
        private TaskCompletionSource<bool> startLifecycleCompletion;

        public ProtocolStarter(
            NetworkingProtocol protocol,
            ILifecycleMessaging lifecycle)
        {
            this.protocol = protocol;
            this.lifecycle = lifecycle;
        }

        public int? TargetClientId { get; set; }

        public bool SendToServer { get => !this.TargetClientId.HasValue; }

        public TimeSpan StartTimeout { get; } = TimeSpan.FromSeconds(5);

        public virtual async Task StartAsync()
        {
            this.Cancel();
            this.startLifecycleCompletion = new TaskCompletionSource<bool>();

            var message = new StartProtocolRequestMessage
            {
                ProtocolName = this.protocol.Name,
            };

            var writeFunc = new Action<OutgoingMessage>(message.WriteTo);

            if (this.SendToServer)
            {
                this.lifecycle.SendLifecycleMessageToServer(this.protocol.CorrelationId, LifecycleMessageType.StartProtocol, writeFunc);
            }
            else
            {
                this.lifecycle.SendLifecycleMessageToClient(this.protocol.CorrelationId, LifecycleMessageType.StartProtocol, this.TargetClientId.Value, writeFunc);
            }

            if (await Task.WhenAny(this.startLifecycleCompletion.Task, Task.Delay(this.StartTimeout)).ConfigureAwait(false) != this.startLifecycleCompletion.Task)
            {
                throw new ProtocolTimeoutException();
            }

            this.startLifecycleCompletion.Task.Wait();
        }

        public void Cancel()
        {
            this.startLifecycleCompletion?.TrySetCanceled();
        }

        internal virtual void OnProtocolStartResponse(StartProtocolResponseMessage response)
        {
            if (response.Succeeded)
            {
                this.startLifecycleCompletion.TrySetResult(true);
                return;
            }

            switch (response.ErrorCode.Value)
            {
                case ProtocolError.DuplicatedCorrelationId:
                    this.startLifecycleCompletion.TrySetException(new DuplicatedProtocolStartException());
                    break;
                case ProtocolError.MissingProtocolInstantiator:
                    this.startLifecycleCompletion.TrySetException(new ProtocolNotRegisteredException());
                    break;
            }
        }
    }
}
