// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.Properties.Session;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Core.Modules.Networking
{
    /// <summary>
    /// Controls module synchronization data, mostly related with module
    /// activation/deactivation status.
    /// </summary>
    /// <typeparam name="TModuleData">Module type.</typeparam>
    public abstract class ModuleSessionSync<TModuleData> : DataGroupSynchronization<TModuleData>
        where TModuleData : ModuleSessionData
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private ModuleActivationSync moduleActivationSync = null;

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
        protected sealed override void OnDataSynchronized(TModuleData data)
        {
            using (this.logger?.BeginScope("Ruler session synchronization"))
            {
                this.logger?.LogDebug($"Detected update in session data");
                this.logger?.LogDebug($"Visibility key: {data.VisibilityPropertyKey}");
                this.moduleActivationSync.PropertyKeyByte = data.VisibilityPropertyKey;
                this.OnModuleDataSynchronized(data);
            }
        }

        /// <summary>
        /// Invoked when associated module data has been synchronized from server due
        /// to an update.
        /// </summary>
        /// <param name="data">Module data.</param>
        protected abstract void OnModuleDataSynchronized(TModuleData data);

        /// <inheritdoc/>
        protected override void OnSessionDisconnected()
        {
            this.moduleActivationSync.Module.Run(false);

            var moduleActivationKey = this.Owner.FindComponent<ModuleActivationNetworkKey<TModuleData>>();
            moduleActivationKey?.Reset();
        }
    }
}
