using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;

namespace Xrv.Core.UI.Tabs
{
    internal class TabItemAssociation : Component
    {
        private bool isSelected;
        private Color unselectedTextColor;
        private Color selectedTextColor;

        [BindComponent(source: BindComponentSource.Parents)]
        protected TabControl tabControl;

        [BindComponent]
        protected StandardButtonConfigurator configurator;

        [BindComponent(source: BindComponentSource.Children)]
        protected PressableButton button;

        [IgnoreEvergine]
        public TabItem Item { get; set; }

        [IgnoreEvergine]
        public bool IsSelected
        {
            get => this.isSelected;

            set
            {
                if (this.isSelected != value)
                {
                    this.isSelected = value;
                    this.UpdateTextColor();
                }
            }
        }

        public Color UnselectedTextColor
        {
            get => this.unselectedTextColor;

            set
            {
                if (this.unselectedTextColor != value)
                {
                    this.unselectedTextColor = value;
                    this.UpdateTextColor();
                }
            }
        }

        public Color SelectedTextColor
        {
            get => this.selectedTextColor;

            set
            {
                if (this.selectedTextColor != value)
                {
                    this.selectedTextColor = value;
                    this.UpdateTextColor();
                }
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateTextColor();
            this.button.ButtonReleased += this.Button_ButtonReleased;
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.button.ButtonReleased -= this.Button_ButtonReleased;
        }

        private void UpdateTextColor()
        {
            if (this.IsAttached)
            {
                configurator.PrimaryColor = this.isSelected ? this.selectedTextColor : this.unselectedTextColor;
            }
        }

        private void Button_ButtonReleased(object sender, EventArgs args) =>
            this.tabControl.SelectedItem = this.Item;
    }
}
