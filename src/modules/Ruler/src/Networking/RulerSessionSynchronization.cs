// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Threading;
using Microsoft.Extensions.Logging;
using Xrv.Core;
using Xrv.Core.Modules;
using Xrv.Core.Networking.Properties.Session;

namespace Xrv.Ruler.Networking
{
    internal class RulerSessionSynchronization : DataGroupSynchronization<RulerSessionData>
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private ModuleActivationSync moduleActivationSync = null;

        private ILogger logger;

        public override string GroupName => nameof(RulerModule);

        public override RulerSessionData CreateInitialInstance() => new RulerSessionData();

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        protected override void OnDataSynchronized(RulerSessionData data)
        {
            using (this.logger?.BeginScope("Ruler session synchronization"))
            {
                this.logger?.LogDebug($"Detected update in session data");
                this.logger?.LogDebug($"Visibility key: {data.VisibilityPropertyKey}");
                this.logger?.LogDebug($"Handle1 key: {data.Handle1PropertyKey}");
                this.logger?.LogDebug($"Handle2 key: {data.Handle2PropertyKey}");
                this.moduleActivationSync.PropertyKeyByte = data.VisibilityPropertyKey;

                _ = EvergineForegroundTask.Run(() =>
                {
                    var rulerKeys = this.Managers.EntityManager.FindFirstComponentOfType<RulerKeysAssignation>();
                    rulerKeys?.SetKeys(new byte[] { data.Handle1PropertyKey, data.Handle2PropertyKey });
                });
            }
        }

        protected override void OnSessionDisconnection()
        {
            var moduleActivationKey = this.Managers.EntityManager.FindFirstComponentOfType<RulerModuleActivationKey>();
            moduleActivationKey?.Reset();

            var rulerKeys = this.Managers.EntityManager.FindFirstComponentOfType<RulerKeysAssignation>();
            rulerKeys?.Reset();
        }
    }
}
