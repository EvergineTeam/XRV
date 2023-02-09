// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Linq;
using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Themes.Texts;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Base class for dialogs.
    /// </summary>
    public abstract class Dialog : Window
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        /// <summary>
        /// Buttons holder entity.
        /// </summary>
        protected Entity buttonsHolder;

        /// <summary>
        /// Cancel button holder.
        /// </summary>
        protected Entity cancelHolder;

        /// <summary>
        /// Accept button holder.
        /// </summary>
        protected Entity acceptHolder;

        /// <summary>
        /// Single option button holder.
        /// </summary>
        protected Entity singleButtonHolder;

        private Dictionary<PressableButton, string> options;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dialog"/> class.
        /// </summary>
        public Dialog()
        {
            this.options = new Dictionary<PressableButton, string>();
        }

        /// <summary>
        /// Gets or sets dialog result.
        /// </summary>
        [IgnoreEvergine]
        public string Result { get; protected set; }

        /// <summary>
        /// Adds an option to the dialog.
        /// </summary>
        /// <param name="button">Button instance.</param>
        /// <param name="option">Option instance.</param>
        public void AddOption(PressableButton button, DialogOption option)
        {
            this.options.Add(button, option.Key);
            button.ButtonReleased += this.Button_ButtonReleased;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.Configurator.UpdateContent();
            this.cancelHolder = this.Owner.FindChildrenByTag("PART_base_dialog_cancel_holder", true).First();
            this.acceptHolder = this.Owner.FindChildrenByTag("PART_base_dialog_accept_holder", true).First();
            this.singleButtonHolder = this.Owner.FindChildrenByTag("PART_base_dialog_single_holder", true).First();

            if (!this.options.Any())
            {
                this.InstantiateOptions();
            }
        }

        /// <summary>
        /// Instantiates buttons.
        /// </summary>
        protected abstract void InstantiateOptions();

        /// <summary>
        /// Clears dialog options.
        /// </summary>
        protected virtual void Clear()
        {
            this.cancelHolder?.RemoveAllChildren();
            this.acceptHolder?.RemoveAllChildren();
            this.singleButtonHolder?.RemoveAllChildren();

            this.UnsubscribeEvents();
            this.options.Clear();
        }

        /// <summary>
        /// Creates a button instance from option.
        /// </summary>
        /// <param name="option">Option model.</param>
        /// <returns>Button entity.</returns>
        protected virtual Entity CreateButtonInstance(DialogOption option)
        {
            var prefab = this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.TextButton);
            var buttonInstance = prefab.Instantiate();
            buttonInstance
                .AddComponent(option.Configuration)
                .AddComponent(new ButtonTextStyle
                {
                    TextStyleKey = DefaultTextStyles.XrvPrimary2Size2,
                });

            return buttonInstance;
        }

        /// <inheritdoc/>
        protected override float GetOpenDistance()
        {
            var distances = this.xrvService.WindowsSystem.Distances;
            return distances.GetDistanceOrAlternative(this.DistanceKey, Distances.NearKey);
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
