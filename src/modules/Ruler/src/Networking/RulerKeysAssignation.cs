// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Xrv.Core.Networking.Extensions;
using Xrv.Core.Networking.Properties;
using Xrv.Core.Networking.Properties.KeyRequest;

namespace Xrv.Ruler.Networking
{
    /// <summary>
    /// Networking keys assignation for ruler handles.
    /// </summary>
    public class RulerKeysAssignation : KeysAssignation
    {
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle1")]
        private TransformSynchronization handle1Sync = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_ruler_handle2")]
        private TransformSynchronization handle2Sync = null;

        [BindComponent(source: BindComponentSource.Scene, isRequired: false)]
        private RulerSessionSynchronization session = null;

        /// <summary>
        /// Explicitily assign keys to handles.
        /// </summary>
        /// <param name="keys">Keys to be assigned.</param>
        public void SetKeys(byte[] keys)
        {
            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerKeysAssignation)}] Handle keys: {keys[0]}, {keys[1]}");
            this.handle1Sync.PropertyKeyByte = keys[0];
            this.handle2Sync.PropertyKeyByte = keys[1];
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

            System.Diagnostics.Debug.WriteLine($"[{nameof(RulerKeysAssignation)}] Got result of {keys.Length} keys: {keys[0]}, {keys[1]}");
            this.SetKeys(keys);
            this.session?.UpdateData(data =>
            {
                data.Handle1PropertyKey = this.handle1Sync.PropertyKeyByte;
                data.Handle2PropertyKey = this.handle2Sync.PropertyKeyByte;
            });
        }
    }
}
