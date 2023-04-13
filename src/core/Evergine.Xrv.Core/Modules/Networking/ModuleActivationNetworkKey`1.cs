// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.Extensions;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Modules.Networking
{
    /// <summary>
    /// It controls networking keys assignation for module activation/deactivation.
    /// </summary>
    /// <typeparam name="TModuleData">Module type.</typeparam>
    public class ModuleActivationNetworkKey<TModuleData> : KeysAssignation
        where TModuleData : ModuleSessionData
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private ModuleActivationSync moduleActivation = null;

        [BindComponent(isExactType: false)]
        private ModuleSessionSync<TModuleData> session = null;

        private ILogger logger;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnKeysAssigned(byte[] keys)
        {
            if (keys == null)
            {
                this.moduleActivation.ResetKey();
                return;
            }

            var visibilityKey = keys[0];
            this.logger?.LogDebug($"{this.moduleActivation.Module.Name} module activation: key {visibilityKey} assigned for visibility");
            this.session.UpdateData(data =>
            {
                data.VisibilityPropertyKey = keys[0];
            });
        }

        /// <inheritdoc/>
        protected override void AssignKeysToProperties()
        {
            this.moduleActivation.PropertyKeyByte = this.AssignedKeys[0];
        }
    }
}
