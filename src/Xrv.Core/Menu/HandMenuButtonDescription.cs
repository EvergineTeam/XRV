// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.Menu
{
    public class HandMenuButtonDescription
    {
        public HandMenuButtonDescription()
        {
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public bool IsToggle { get; set; }

        public Guid IconOn { get; set; }

        public Guid IconOff { get; set; }

        public string TextOn { get; set; }

        public string TextOff { get; set; }
    }
}
