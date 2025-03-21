﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Xrv.Core.Services.Messaging;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.Menu
{
    /// <summary>
    /// Notifies when a hand menu button has been pressed.
    /// </summary>
    public class HandMenuButtonPress : PubSubOnButtonPress<HandMenuActionMessage>
    {
        /// <summary>
        /// Gets or sets button description.
        /// </summary>
        [IgnoreEvergine]
        public ButtonDescription Description { get; set; }

        /// <inheritdoc/>
        protected override HandMenuActionMessage GetPublishData(bool isOn) => new HandMenuActionMessage
        {
            Description = this.Description,
        };
    }
}
