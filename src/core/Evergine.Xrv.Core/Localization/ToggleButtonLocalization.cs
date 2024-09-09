// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.States;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Linq;

namespace Evergine.Xrv.Core.Localization
{
    /// <summary>
    /// Controls localization for <see cref="ToggleButton"/>.
    /// </summary>
    [AllowMultipleInstances]
    public class ToggleButtonLocalization : BaseLocalization
    {
        [BindComponent(source: BindComponentSource.Children)]
        private ToggleStateManager toggleManager = null;

        /// <summary>
        /// Gets or sets target state associated to toggle button that this component
        /// will provide localized text to.
        /// </summary>
        public ToggleState TargetState { get; set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.toggleManager.StateChanged += this.ToggleManager_StateChanged;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.toggleManager.StateChanged -= this.ToggleManager_StateChanged;
        }

        /// <inheritdoc/>
        protected override bool ShouldUpdateText() =>
            base.ShouldUpdateText() &&
            this.TargetState == this.toggleManager.CurrentState?.Value;

        /// <inheritdoc/>
        protected override void SetText(string text)
        {
            var configurator = this.FindConfiguratorForThisState();
            if (configurator != null)
            {
                configurator.Text = text;
            }
        }

        private ToggleButtonConfigurator FindConfiguratorForThisState()
        {
            var configurator = this.Owner
                .FindComponentsInChildren<ToggleButtonConfigurator>(isExactType: false)
                .FirstOrDefault(configurator => configurator.TargetState == this.TargetState);

            return configurator;
        }

        private void ToggleManager_StateChanged(object sender, StateChangedEventArgs<ToggleState> args)
        {
            if (this.TargetState == args.NewState.Value)
            {
                this.RequestUpdate();
            }
        }
    }
}
