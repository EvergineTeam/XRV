// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Xrv.Core.Services.Messaging;

namespace Evergine.Xrv.Core.Modules
{
    internal class ActivateModuleOnButtonPress : PubSubOnButtonPress<ActivateModuleMessage>
    {
        public ActivateModuleOnButtonPress(Module module)
        {
            this.Module = module;
        }

        [IgnoreEvergine]
        public Module Module { get; private set; }

        internal void SetModuleActivationState(bool isOn) => this.NotifyChange(isOn);

        protected override ActivateModuleMessage GetPublishData(bool isOn) =>
            new ActivateModuleMessage
            {
                Module = this.Module,
                IsOn = isOn,
            };
    }
}
