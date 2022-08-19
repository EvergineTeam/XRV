using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Core.Extensions;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.UI.Dialogs
{
    public abstract class Dialog : Window
    {
        private Dictionary<PressableButton, string> options;

        [BindService]
        protected AssetsService assetsService;

        protected Entity buttonsHolder;
        protected Entity cancelHolder;
        protected Entity acceptHolder;
        protected Entity singleButtonHolder;

        [IgnoreEvergine]
        public string Result { get; protected set; }

        public Dialog()
        {
            this.DestroyOnClose = true;
            this.options = new Dictionary<PressableButton, string>();
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            this.cancelHolder = this.Owner.FindChildrenByTag("PART_base_dialog_cancel_holder", true).First();
            this.acceptHolder = this.Owner.FindChildrenByTag("PART_base_dialog_accept_holder", true).First();
            this.singleButtonHolder = this.Owner.FindChildrenByTag("PART_base_dialog_single_holder", true).First();
            this.InstantiateOptions();
        }

        protected override void OnDeactivated()
        {
            this.Clear();
            base.OnDeactivated();
        }

        public void AddOption(PressableButton button, DialogOption option)
        {
            this.options.Add(button, option.Key);
            button.ButtonReleased += this.Button_ButtonReleased;
        }

        protected abstract void InstantiateOptions();

        protected virtual void Clear()
        {
            this.cancelHolder?.RemoveAllChildren();
            this.acceptHolder?.RemoveAllChildren();
            this.singleButtonHolder?.RemoveAllChildren();

            this.UnsubscribeEvents();
            this.options.Clear();
        }

        protected virtual Entity CreateButtonInstance(DialogOption option)
        {
            var prefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
            var buttonInstance = prefab.Instantiate();
            buttonInstance.AddComponent(option.Configuration);
            Workarounds.MrtkRotateButton(buttonInstance);

            return buttonInstance;
        }

        private void Button_ButtonReleased(object sender, EventArgs args)
        {
            if (sender is PressableButton button)
            {
                this.Result = this.options[button];
                this.Close();
            }
        }

        private void UnsubscribeEvents()
        {
            foreach (var item in this.options)
            {
                PressableButton button = item.Key;
                button.ButtonReleased -= this.Button_ButtonReleased;
            }
        }
    }
}
