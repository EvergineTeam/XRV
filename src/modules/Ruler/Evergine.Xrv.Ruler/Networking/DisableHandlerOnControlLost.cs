// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.Networking.ControlRequest;

namespace Evergine.Xrv.Ruler.Networking
{
    /// <summary>
    /// Disables ruler handler on session control lost.
    /// </summary>
    public class DisableHandlerOnControlLost : SessionControlChangeObserver
    {
        [BindComponent]
        private RulerHandlerBehavior handler = null;

        /// <inheritdoc/>
        protected override void OnControlGained()
        {
            base.OnControlGained();
            this.handler.IsEnabled = true;
        }

        /// <inheritdoc/>
        protected override void OnControlLost()
        {
            base.OnControlLost();
            this.handler.IsEnabled = false;
        }
    }
}
