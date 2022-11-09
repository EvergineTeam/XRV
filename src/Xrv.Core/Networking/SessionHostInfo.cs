// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Networking;

namespace Xrv.Core.Networking
{
    /// <summary>
    /// Session host information model.
    /// </summary>
    public class SessionHostInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionHostInfo"/> class.
        /// </summary>
        /// <param name="name">Host name.</param>
        /// <param name="endpoint">Host endpoint.</param>
        public SessionHostInfo(string name, NetworkEndpoint endpoint)
        {
            this.Name = name;
            this.Endpoint = endpoint;
        }

        /// <summary>
        /// Gets host name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets host endpoint.
        /// </summary>
        public NetworkEndpoint Endpoint { get; private set; }

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            (obj as SessionHostInfo)?.Endpoint.Equals(this.Endpoint) == true;

        /// <inheritdoc/>
        public override int GetHashCode() => this.Endpoint.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => $"{this.Name} - {this.Endpoint}";
    }
}
