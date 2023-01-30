// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Networking.Properties.KeyRequest
{
    internal class KeyRegister
    {
        public byte Key { get; internal set; }

        public int ReservedByClientId { get; internal set; }

        public Guid CorrelationId { get; internal set; }

        public DateTime? ExpiresOn { get; internal set; }

        public bool IsConfirmed { get => !this.ExpiresOn.HasValue; }
    }
}
