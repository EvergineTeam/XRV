// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Xrv.Core.Modules;
using Xrv.Core.Networking.Properties.Session;

namespace Xrv.Ruler.Networking
{
    internal class RulerSessionSynchronization : DataGroupSynchronization<RulerSessionData>
    {
        [BindComponent]
        private ModuleActivationSync moduleActivationSync = null;

        public override string GroupName => nameof(RulerModule);

        public override RulerSessionData CreateInitialInstance() => new RulerSessionData();

        protected override void OnDataSynchronized(RulerSessionData data)
        {
            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerSessionSynchronization)}] Detected update in session data");
            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerSessionSynchronization)}] visibility key: {data.VisibilityPropertyKey}");
            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerSessionSynchronization)}] handle1 key: {data.Handle1PropertyKey}");
            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerSessionSynchronization)}] handle2 key: {data.Handle2PropertyKey}");
            this.moduleActivationSync.PropertyKeyByte = data.VisibilityPropertyKey;

            var rulerKeys = this.Managers.EntityManager.FindFirstComponentOfType<RulerKeysAssignation>();
            rulerKeys?.SetKeys(new byte[] { data.Handle1PropertyKey, data.Handle2PropertyKey });
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
