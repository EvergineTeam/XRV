// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.Networking.ControlRequest
{
    /// <summary>
    /// Changes button visual enable/disable state depending on current
    /// session control status.
    /// </summary>
    public class ButtonEnabledStateByControlStatus : SessionControlChangeObserver
    {
        [BindComponent(source: BindComponentSource.Children)]
        private VisuallyEnabledController visuallyEnabled = null;

        /// <summary>
        /// Gets or sets a value indicating whether button should be enabled when
        /// control is gained, or vice-versa.
        /// </summary>
        public bool EnableWhenGained { get; set; } = true;

        /// <inheritdoc/>
        protected override void OnControlGained()
        {
            base.OnControlGained();
            this.visuallyEnabled.IsVisuallyEnabled = true;
        }

        /// <inheritdoc/>
        protected override void OnControlLost()
        {
            base.OnControlLost();
            this.visuallyEnabled.IsVisuallyEnabled = false;
        }
    }
}
