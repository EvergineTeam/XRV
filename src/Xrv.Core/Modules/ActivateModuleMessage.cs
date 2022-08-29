﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Modules
{
    public class ActivateModuleMessage
    {
        public Module Module { get; set; }

        public bool IsOn { get; set; }
    }
}
