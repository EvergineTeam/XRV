// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Xrv.Core.Messaging;

namespace Xrv.Core.Modules
{
    internal class ActivateModuleOnButtonPress : PubSubOnButtonPress<ActivateModuleMessage>
    {
        [IgnoreEvergine]
        public Module Module { get; set; }

        internal void SetModuleActivationState(bool isOn) => this.NotifyChange(isOn);

        protected override ActivateModuleMessage GetPublishData(bool isOn) =>
            new ActivateModuleMessage
            {
                Module = this.Module,
                IsOn = isOn,
            };
    }
}
