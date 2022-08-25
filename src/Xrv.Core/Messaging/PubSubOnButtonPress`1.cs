// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;

namespace Xrv.Core.Messaging
{
    public abstract class PubSubOnButtonPress<TMessage> : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Children, isExactType: false)]
        private PressableButton button = null;

        [BindComponent(source: BindComponentSource.Children, isExactType: false, isRequired: false)]
        private ToggleButton toggleButton = null;

        protected override void OnActivated()
        {
            base.OnActivated();
            this.button.ButtonReleased += this.Button_ButtonReleased;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.button.ButtonReleased -= this.Button_ButtonReleased;
        }

        protected abstract TMessage GetPublishData(bool isOn);

        private void Button_ButtonReleased(object sender, EventArgs e)
        {
            bool isOn = this.toggleButton?.IsOn ?? true;
            this.xrvService.PubSub.Publish(this.GetPublishData(isOn));
        }
    }
}
