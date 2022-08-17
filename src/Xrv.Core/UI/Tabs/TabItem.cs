using System;

namespace Xrv.Core.UI.Tabs
{
    public class TabItem
    {
        public TabItem()
        {
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public string Text { get; set; }

        public object Data { get; set; }
    }
}
