// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Xrv.Core.Extensions;
using Xrv.Core.UI.Buttons;

namespace Xrv.Core.UI.Tabs
{
    /// <summary>
    /// Component to work with tab control.
    /// </summary>
    public class TabControl : Component
    {
        private readonly ObservableCollection<TabItem> items;
        private readonly Dictionary<Entity, TabItem> mappings;
        private readonly OrderedItemsEnumerator itemsEnumerator;

        private Vector2 size;
        private TabItem selectedItem;
        private Entity contentsContainer;

        private Vector3 defaultCurrentItemPlatePosition;
        private IWorkAction currentItemChangeAnimation;
        private Color inactiveItemTextColor;
        private Color activeItemTextColor;

        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_front_plate")]
        private PlaneMesh frontPlate = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_current_item_plate")]
        private PlaneMesh currentItemPlate = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_current_item_plate")]
        private Transform3D currentItemPlateTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_buttons_container")]
        private Transform3D buttonsContainerTransform = null;

        private Entity buttonsContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabControl"/> class.
        /// </summary>
        public TabControl()
        {
            this.items = new ObservableCollection<TabItem>();
            this.mappings = new Dictionary<Entity, TabItem>();
            this.itemsEnumerator = new OrderedItemsEnumerator(this.mappings);
            this.size = new Vector2(0.3f, 0.2f);
        }

        /// <summary>
        /// Raised when selected item changes.
        /// </summary>
        public event EventHandler<SelectedItemChangedEventArgs> SelectedItemChanged;

        /// <summary>
        /// Gets or sets tab control size.
        /// </summary>
        public Vector2 Size
        {
            get => this.size;
            set
            {
                this.size = value;
                if (this.IsAttached)
                {
                    this.defaultCurrentItemPlatePosition = default;
                    this.UpdateFrontPlateSize();
                }
            }
        }

        /// <summary>
        /// Gets or sets selected item.
        /// </summary>
        [IgnoreEvergine]
        public TabItem SelectedItem
        {
            get => this.selectedItem;
            set
            {
                if (this.selectedItem != value)
                {
                    this.selectedItem = value;
                    this.UpdateSelectedItem();
                }
            }
        }

        /// <summary>
        /// Gets or sets inactive item text color.
        /// </summary>
        public Color InactiveItemTextColor
        {
            get => this.inactiveItemTextColor;

            set
            {
                if (this.inactiveItemTextColor != value)
                {
                    this.inactiveItemTextColor = value;
                    this.UpdateItemsTextColor();
                }
            }
        }

        /// <summary>
        /// Gets or sets active item text color.
        /// </summary>
        public Color ActiveItemTextColor
        {
            get => this.activeItemTextColor;

            set
            {
                if (this.activeItemTextColor != value)
                {
                    this.activeItemTextColor = value;
                    this.UpdateItemsTextColor();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether theme colors should be overriden.
        /// When true, <see cref="ActiveItemTextColor"/> and <see cref="InactiveItemTextColor"/>
        /// will be applied in this case.
        /// </summary>
        public bool OverrideThemeColors { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether content entity should be destroyed
        /// when changing tab. False by default.
        /// </summary>
        public bool DestroyContentOnTabChange { get; set; } = false;

        /// <summary>
        /// Gets list of items.
        /// </summary>
        public IList<TabItem> Items { get => this.items; }

        /// <summary>
        /// Gets builder instance for tab control.
        /// </summary>
        public static TabControlBuilder Builder { get; internal set; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.items.CollectionChanged += this.Items_CollectionChanged;
                this.buttonsContainer = this.Owner.FindChildrenByTag("PART_tab_control_buttons_container", isRecursive: true).First();
                this.contentsContainer = this.Owner.FindChildrenByTag("PART_tab_control_current_item_contents", isRecursive: true).First();
                this.InternalAddItems(this.items); // We can have items added before this component has been attached
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateFrontPlateSize();
            this.ReorderItems();
            this.selectedItem = this.items.FirstOrDefault();
            this.UpdateSelectedItem();
            this.UpdateItemsTextColor();
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.items.CollectionChanged -= this.Items_CollectionChanged;
        }

        private void UpdateFrontPlateSize()
        {
            if (!this.IsAttached || !this.CheckCorrectSizes())
            {
                return;
            }

            this.frontPlate.Width = this.size.X - (this.currentItemPlate.Width / 2);
            this.frontPlate.Height = this.size.Y;
            var currentItemPosition = this.currentItemPlateTransform.LocalPosition;
            currentItemPosition.X = (-this.size.X / 2) - 0.017f;
            currentItemPosition.Y = (this.size.Y / 2) - (this.currentItemPlate.Height / 2);
            this.currentItemPlateTransform.LocalPosition = currentItemPosition;

            if (this.defaultCurrentItemPlatePosition == default)
            {
                this.defaultCurrentItemPlatePosition = this.currentItemPlateTransform.LocalPosition;
            }

            var buttonsPosition = this.buttonsContainerTransform.LocalPosition;
            buttonsPosition.X = currentItemPosition.X;
            buttonsPosition.Y = currentItemPosition.Y;
            this.buttonsContainerTransform.LocalPosition = buttonsPosition;
        }

        private bool CheckCorrectSizes() =>
            this.size.X != 0
            && this.size.Y != 0;

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.InternalAddItems(args.NewItems.OfType<TabItem>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.InternalRemoveItems(args.OldItems.OfType<TabItem>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.InternalClearItems();
                    break;
            }

            this.ReorderItems();
        }

        private void InternalAddItems(IEnumerable<TabItem> items)
        {
            var buttonsFactory = new TabItemButtonFactory(this.assetsService);
            foreach (var item in items)
            {
                var buttonInstance = buttonsFactory.CreateInstance(item);
                var transform = buttonInstance.FindComponent<Transform3D>();

                var pressableButton = buttonInstance.FindComponentInChildren<PressableButton>();
                pressableButton.ButtonReleased += this.PressableButton_ButtonReleased;

                this.buttonsContainer.AddChild(buttonInstance);
                this.mappings.Add(buttonInstance, item);
            }

            this.UpdateItemsTextColor();
            this.ReorderItems();
        }

        private void InternalRemoveItems(IEnumerable<TabItem> items)
        {
            bool currentItemRemoved = false;
            int currentItemIndex = -1;

            for (int i = 0; i < this.mappings.Count(); i++)
            {
                Entity owner = this.mappings.ElementAt(i).Key;
                TabItem item = this.mappings.ElementAt(i).Value;

                var pressableButton = owner.FindComponentInChildren<PressableButton>();
                if (pressableButton != null)
                {
                    pressableButton.ButtonReleased -= this.PressableButton_ButtonReleased;
                }

                if (items.Contains(item))
                {
                    this.Managers.EntityManager.Remove(owner);
                    this.mappings.Remove(owner);

                    if (item == this.selectedItem)
                    {
                        currentItemRemoved = true;
                        currentItemIndex = i;
                    }
                }
            }

            this.ReorderItems();

            if (currentItemRemoved)
            {
                var newSelectedIndex = Math.Max(0, Math.Min(currentItemIndex - 1, this.Items.Count - 1));
                this.SelectedItem = this.items.Any() ? this.items.ElementAt(newSelectedIndex) : null;
                this.UpdateItemsTextColor();
            }
        }

        private void UpdateSelectedItem()
        {
            var itemIndex = -1;

            int i = 0;
            foreach (var child in this.itemsEnumerator)
            {
                TabItem currentItem = this.mappings.ContainsKey(child) ? this.mappings[child] : null;
                if (currentItem == this.selectedItem)
                {
                    itemIndex = i;
                    break;
                }

                i++;
            }

            if (itemIndex != -1)
            {
                this.currentItemChangeAnimation?.Cancel();

                var animationDuration = TimeSpan.FromMilliseconds(300);
                var toPosition = new Vector3
                {
                    X = this.defaultCurrentItemPlatePosition.X,
                    Y = this.defaultCurrentItemPlatePosition.Y - (itemIndex * ButtonConstants.SquareButtonSize),
                    Z = this.defaultCurrentItemPlatePosition.Z,
                };

                this.currentItemChangeAnimation = new Vector3AnimationWorkAction(
                    this.Owner,
                    this.currentItemPlateTransform.LocalPosition,
                    toPosition,
                    animationDuration,
                    EaseFunction.CubicOutEase,
                    @value =>
                    {
                        var position = this.currentItemPlateTransform.LocalPosition;
                        position.X = @value.X;
                        position.Y = @value.Y;
                        position.Z = @value.Z;
                        this.currentItemPlateTransform.LocalPosition = position;
                    });
                this.currentItemChangeAnimation.Run();
            }

            this.UpdateContent();
            this.SelectedItemChanged?.Invoke(this, new SelectedItemChangedEventArgs(this.selectedItem));
        }

        private void InternalClearItems() => this.buttonsContainer.RemoveAllChildren();

        private void UpdateContent()
        {
            if (this.DestroyContentOnTabChange)
            {
                this.contentsContainer.RemoveAllChildren();
            }
            else
            {
                foreach (var child in this.contentsContainer.ChildEntities)
                {
                    child.IsEnabled = false;
                }
            }

            if (this.selectedItem == null)
            {
                return;
            }

            var content = this.selectedItem.Contents?.Invoke();
            if (content == null)
            {
                return;
            }

            if (!this.contentsContainer.ChildEntities.Contains(content))
            {
                this.contentsContainer.AddChild(content);
            }

            content.IsEnabled = true;
        }

        private void ReorderItems()
        {
            if (this.IsAttached)
            {
                int i = 0;
                foreach (var child in this.itemsEnumerator)
                {
                    var transform = child.FindComponent<Transform3D>();
                    var position = transform.LocalPosition;
                    position.Y = -i * ButtonConstants.SquareButtonSize;
                    transform.LocalPosition = position;
                    i++;
                }

                this.currentItemPlate.Owner.IsEnabled = this.items.Any() || Application.Current.IsEditor;
            }
        }

        private void UpdateItemsTextColor()
        {
            if (!this.IsAttached)
            {
                return;
            }

            for (int i = 0; i < this.buttonsContainer.ChildEntities.Count(); i++)
            {
                var child = this.buttonsContainer.ChildEntities.ElementAt(i);
                var configurator = child.FindComponent<StandardButtonConfigurator>();
                if (configurator != null)
                {
                    bool isSelected = this.mappings.ContainsKey(child) ? this.mappings[child] == this.selectedItem : false;
                    configurator.PrimaryColor = this.GetTextColor(isSelected);
                }
            }
        }

        private Color GetTextColor(bool isSelected)
        {
            Color overridenColor = isSelected ? this.activeItemTextColor : this.inactiveItemTextColor;

            var theme = this.xrvService.ThemesSystem?.CurrentTheme;
            if (theme == null || this.OverrideThemeColors)
            {
                return overridenColor;
            }

            return isSelected ? theme.PrimaryColor3 : theme.SecondaryColor1;
        }

        private void PressableButton_ButtonReleased(object sender, EventArgs e)
        {
            if (sender is PressableButton button)
            {
                var owner = button.Owner.FindComponentInParents<StandardButtonConfigurator>().Owner;
                if (this.mappings.ContainsKey(owner))
                {
                    this.SelectedItem = this.mappings[owner];
                    this.UpdateItemsTextColor();
                }
            }
        }

        private class OrderedItemsEnumerator : IEnumerable<Entity>
        {
            private readonly Dictionary<Entity, TabItem> mappings;

            public OrderedItemsEnumerator(Dictionary<Entity, TabItem> mappings)
            {
                this.mappings = mappings;
            }

            public IEnumerator<Entity> GetEnumerator()
            {
                var orderedEntities = this.mappings
                        .OrderBy(map => map.Value.Order)
                        .Select(map => map.Key);

                foreach (var item in orderedEntities)
                {
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
