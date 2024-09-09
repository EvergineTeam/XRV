// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.Networking.Extensions;
using Evergine.Xrv.Core.Networking.Properties;
using Evergine.Xrv.Core.Networking.Properties.KeyRequest;
using Microsoft.Extensions.Logging;

namespace Evergine.Xrv.Ruler.Networking
{
    /// <summary>
    /// Networking keys assignation for ruler handles.
    /// </summary>
    public class RulerKeysAssignation : KeysAssignation
    {
        [BindService]
        private XrvService xrvService = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_ruler_handle1")]
        private Entity handle1Entity = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_ruler_handle2")]
        private Entity handle2Entity = null;

        [BindComponent(source: BindComponentSource.Scene, isRequired: false)]
        private RulerSessionSynchronization session = null;

        private TransformSynchronization handle1Sync = null;
        private TransformSynchronization handle2Sync = null;

        private ILogger logger = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.handle1Sync = this.handle1Entity.FindComponentInChildren<TransformSynchronization>(isRecursive: true);
                this.handle2Sync = this.handle2Entity.FindComponentInChildren<TransformSynchronization>(isRecursive: true);
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnKeysAssigned(byte[] keys)
        {
            if (keys == null)
            {
                this.handle1Sync?.ResetKey();
                this.handle2Sync?.ResetKey();
                return;
            }

            this.logger?.LogDebug($"Ruler keys assigned. Result of {keys.Length} keys: {keys[0]}, {keys[1]}");
            this.session?.UpdateData(data =>
            {
                this.logger?.LogDebug($"Updating session data for ruler keys: {keys[0]}, {keys[1]}");
                data.Handle1PropertyKey = keys[0];
                data.Handle2PropertyKey = keys[1];
            });
        }

        /// <inheritdoc/>
        protected override void AssignKeysToProperties()
        {
            var keys = this.AssignedKeys;
            this.logger?.LogDebug($"Ruler keys established: {keys[0]}, {keys[1]}");
            this.handle1Sync.PropertyKeyByte = keys[0];
            this.handle2Sync.PropertyKeyByte = keys[1];
        }
    }
}
