// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking.Connection.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.Messaging
{
    /// <summary>
    /// Base class for network protocols.
    /// </summary>
    public abstract class NetworkingProtocol
    {
        /// <summary>
        /// Client server communication.
        /// </summary>
        protected readonly IClientServerMessaging ClientServer;

        /// <summary>
        /// Target client identifier.
        /// </summary>
        protected int? targetClientId;

        private readonly ILogger logger;
        private readonly ILifecycleMessaging lifecycle;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingProtocol"/> class.
        /// </summary>
        /// <param name="network">Network system.</param>
        /// <param name="logger">Logger.</param>
        protected NetworkingProtocol(NetworkSystem network, ILogger logger)
        {
            this.logger = logger;
            this.ClientServer = network.ClientServerMessaging;
            this.lifecycle = network.ClientServerMessaging;
        }

        internal event EventHandler Ended;

        /// <summary>
        /// Gets protocol name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets correlation identifier. It uniquely identifies running
        /// protocol instances.
        /// </summary>
        public Guid CorrelationId { get; internal set; } = Guid.NewGuid();

        internal virtual ProtocolStarter ProtocolStarter { get; set; }

        internal virtual void InternalMessageReceived(INetworkingMessageConverter message, int senderId)
        {
            using (this.logger?.BeginScope("{ProtocolName}", this.Name))
            using (this.logger?.BeginScope("{CorrelationId}", this.CorrelationId))
            {
                this.OnMessageReceived(message, senderId);
            }
        }

        internal Task InternalStartProtocolAsync() => this.StartProtocolAsync();

        internal void InternalEndProtocol() => this.EndProtocol();

        internal virtual INetworkingMessageConverter InternalCreateMessageInstance(IIncomingMessage message) =>
            this.CreateMessageInstance(message);

        /// <summary>
        /// Executes protocol, from lifecycle start to end. It also runs
        /// protocol-specific logic passed as argument.
        /// </summary>
        /// <param name="logic">Protocol-specific logic.</param>
        /// <returns>A task.</returns>
        protected async Task ExecuteAsync(Func<Task> logic)
        {
            using (this.logger?.BeginScope("{ProtocolName}", this.Name))
            using (this.logger?.BeginScope("{CorrelationId}", this.CorrelationId))
            {
                try
                {
                    await this.StartProtocolAsync().ConfigureAwait(false);
                    await logic.Invoke().ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    this.EndProtocol();
                }
            }
        }

        /// <summary>
        /// Starts protocol execution.
        /// </summary>
        /// <returns>A task.</returns>
        protected Task StartProtocolAsync()
        {
            using (this.logger?.BeginScope("Protocol start"))
            {
                if (this.ProtocolStarter == null)
                {
                    this.ProtocolStarter = new ProtocolStarter(this, this.lifecycle);
                }

                this.ProtocolStarter.TargetClientId = this.targetClientId;
                this.ClientServer.RegisterSelfProtocol(this);

                this.logger?.LogDebug($"Starting protocol {this.Name} with correlation: {this.CorrelationId}");
                return this.ProtocolStarter.StartAsync();
            }
        }

        /// <summary>
        /// Ends a protocol execution.
        /// </summary>
        /// <param name="beforeSending">Transform message before being sent.</param>
        protected void EndProtocol(Action<OutgoingMessage> beforeSending = null)
        {
            this.ProtocolStarter.Cancel();
            this.ClientServer.UnregisterSelfProtocol(this);

            if (this.ClientServer.IsConnected)
            {
                if (this.ProtocolStarter.TargetClientId.HasValue)
                {
                    this.lifecycle.SendLifecycleMessageToClient(
                        this.CorrelationId,
                        LifecycleMessageType.EndProtocol,
                        this.ProtocolStarter.TargetClientId.Value,
                        beforeSending);
                }
                else
                {
                    this.lifecycle.SendLifecycleMessageToServer(
                        this.CorrelationId,
                        LifecycleMessageType.EndProtocol,
                        beforeSending);
                }
            }

            this.Ended?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="senderId">Sender identifier.</param>
        protected abstract void OnMessageReceived(INetworkingMessageConverter message, int senderId);

        /// <summary>
        /// Creates a protocol-specific message instance.
        /// </summary>
        /// <param name="message">Incoming message.</param>
        /// <returns>Protocol-specific message instance.</returns>
        protected abstract INetworkingMessageConverter CreateMessageInstance(IIncomingMessage message);
    }
}
