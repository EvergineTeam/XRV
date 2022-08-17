using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;

namespace Xrv.Core.Modules
{
    internal class ActivateModuleOnButtonRelease : Component
    {
        [BindComponent(source: BindComponentSource.Children, isExactType: false)]
        private PressableButton button = null;

        [BindComponent(source: BindComponentSource.Children, isExactType: false, isRequired: false)]
        private ToggleButton toggleButton = null;

        [IgnoreEvergine]
        public Module Module { get; set; }

        protected override void OnActivated()
        {
            base.OnActivated();
            button.ButtonReleased += this.Button_ButtonReleased;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            button.ButtonReleased -= this.Button_ButtonReleased;
        }

        private void Button_ButtonReleased(object sender, EventArgs e)
        {
            bool turnOff = this.toggleButton?.IsOn ?? false;
            this.Module.Run(turnOff);
        }
    }
}
