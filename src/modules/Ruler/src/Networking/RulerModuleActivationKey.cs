// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Xrv.Core.Modules;
using Xrv.Core.Networking.Extensions;
using Xrv.Core.Networking.Properties.KeyRequest;

namespace Xrv.Ruler.Networking
{
    internal class RulerModuleActivationKey : KeysAssignation
    {
        [BindComponent]
        private ModuleActivationSync moduleActivation = null;

        [BindComponent]
        private RulerSessionSynchronization session = null;

        protected override void OnKeysAssigned(byte[] keys)
        {
            if (keys == null)
            {
                this.moduleActivation.ResetKey();
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerModuleActivationKey)}] Got result of {keys.Length} keys");
            this.moduleActivation.PropertyKeyByte = keys[0];
            this.session.UpdateData(data =>
            {
                data.VisibilityPropertyKey = this.moduleActivation.PropertyKeyByte;
            });
        }
    }
}
