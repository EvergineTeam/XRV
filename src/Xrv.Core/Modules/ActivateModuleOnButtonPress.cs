using Evergine.Common.Attributes;
using Xrv.Core.Messaging;

namespace Xrv.Core.Modules
{
    internal class ActivateModuleOnButtonPress : PubSubOnButtonPress<ActivateModuleMessage>
    {
        [IgnoreEvergine]
        public Module Module { get; set; }

        protected override ActivateModuleMessage GetPublishData(bool isOn) =>
            new ActivateModuleMessage
            {
                Module = this.Module,
                IsOn = isOn,
            };
    }
}
