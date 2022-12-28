﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Microsoft.Extensions.Logging;
using Xrv.Core;
using Xrv.Core.Modules;
using Xrv.Core.Networking.Extensions;
using Xrv.Core.Networking.Properties.KeyRequest;

namespace Xrv.Ruler.Networking
{
    internal class RulerModuleActivationKey : KeysAssignation
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private ModuleActivationSync moduleActivation = null;

        [BindComponent]
        private RulerSessionSynchronization session = null;

        private ILogger logger;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        protected override void OnKeysAssigned(byte[] keys)
        {
            if (keys == null)
            {
                this.moduleActivation.ResetKey();
                return;
            }

            this.logger?.LogDebug($"Ruler module activation. Got result of {keys.Length} keys for visibility");
            this.moduleActivation.PropertyKeyByte = keys[0];
            this.session.UpdateData(data =>
            {
                data.VisibilityPropertyKey = this.moduleActivation.PropertyKeyByte;
            });
        }
    }
}
