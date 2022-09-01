// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Xrv.Core.Messaging;

namespace Xrv.Core.Menu
{
    public class HandMenuButtonPress : PubSubOnButtonPress<HandMenuActionMessage>
    {
        [IgnoreEvergine]
        public MenuButtonDescription Description { get; set; }

        /// <inheritdoc/>
        protected override HandMenuActionMessage GetPublishData(bool isOn) => new HandMenuActionMessage
        {
            Description = this.Description,
        };
    }
}
