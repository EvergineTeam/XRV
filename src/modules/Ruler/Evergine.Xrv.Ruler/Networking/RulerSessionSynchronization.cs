// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Modules.Networking;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Ruler.Networking
{
    internal class RulerSessionSynchronization : ModuleSessionSync<RulerSessionData>
    {
        private readonly Entity rulerEntity;

        [BindService]
        private XrvService xrvService = null;

        private ILogger logger;

        public RulerSessionSynchronization(Entity rulerEntity)
        {
            this.rulerEntity = rulerEntity;
        }

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

        protected override void OnModuleDataSynchronized(RulerSessionData data)
        {
            this.logger?.LogDebug($"Handle1 key: {data.Handle1PropertyKey}");
            this.logger?.LogDebug($"Handle2 key: {data.Handle2PropertyKey}");

            var rulerKeys = this.GetKeysAssignationComponent();
            rulerKeys?.SetKeys(new byte[] { data.Handle1PropertyKey, data.Handle2PropertyKey });
        }

        protected override void OnSessionDisconnected()
        {
            base.OnSessionDisconnected();

            var rulerKeys = this.GetKeysAssignationComponent();
            rulerKeys?.Reset();
        }

        private RulerKeysAssignation GetKeysAssignationComponent() =>
            this.rulerEntity.FindComponentInChildren<RulerKeysAssignation>();
    }
}
