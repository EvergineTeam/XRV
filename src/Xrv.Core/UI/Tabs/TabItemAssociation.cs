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
        private Color initialForegroundColor;
        private Color selectedForegroundColor;

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
                    this.UpdateTextColor(this.isSelected ? this.selectedForegroundColor : this.initialForegroundColor);
                }
            }
        }

        public Color SelectedForegroundColor
        {
            get => this.selectedForegroundColor;

            set
            {
                if (this.selectedForegroundColor != value)
                {
                    this.selectedForegroundColor = value;
                    this.UpdateTextColor(this.selectedForegroundColor);
                }
            }
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.initialForegroundColor = this.configurator.PrimaryColor;
            }

            return attached;
        }

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

        private void UpdateTextColor(Color color) => configurator.PrimaryColor = color;

        private void Button_ButtonReleased(object sender, EventArgs args) =>
            this.tabControl.SelectedItem = this.Item;
    }
}
