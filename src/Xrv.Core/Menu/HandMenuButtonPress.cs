using Evergine.Common.Attributes;
using Xrv.Core.Messaging;

namespace Xrv.Core.Menu
{
    public class HandMenuButtonPress : PubSubOnButtonPress<HandMenuActionMessage>
    {
        [IgnoreEvergine]
        public HandMenuButtonDescription Description { get; set; }

        protected override HandMenuActionMessage GetPublishData(bool isOn) => new HandMenuActionMessage
        {
            Description = this.Description,
        };
    }
}
