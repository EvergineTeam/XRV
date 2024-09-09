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
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.UI.Tabs
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
        private int? maxVisibleItems;

        private Vector3 defaultCurrentItemPlatePosition;
        private IWorkAction currentItemChangeAnimation;
        private Color inactiveItemTextColor;
        private Color activeItemTextColor;

        private int currentSkippedItemsCount;

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

        [BindEntity(source: BindEntitySource.Children, tag: "PART_tab_control_more")]
        private Entity moreItemsContainer = null;

        private Entity buttonsContainer;
        private Entity moreItemsUpEntity;
        private Entity moreItemsDownEntity;
        private PressableButton moreItemsUpButton;
        private PressableButton moreItemsDownButton;

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
        /// Gets or sets maximum number of visible items.
        /// </summary>
        public int? MaxVisibleItems
        {
            get => this.maxVisibleItems;

            set
            {
                if (value.HasValue && value < 1)
                {
                    throw new InvalidOperationException($"{nameof(this.MaxVisibleItems)} should be greater than 0");
                }

                if (this.maxVisibleItems != value)
                {
                    this.maxVisibleItems = value;
                    if (this.IsAttached)
                    {
                        this.ReorderItems();
                    }
                }
            }
        }

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
                this.moreItemsUpEntity = this.moreItemsContainer.FindChildrenByTag("PART_tab_control_more_up").First();
                this.moreItemsDownEntity = this.moreItemsContainer.FindChildrenByTag("PART_tab_control_more_down").First();
                this.moreItemsUpButton = this.moreItemsUpEntity.FindComponentInChildren<PressableButton>();
                this.moreItemsDownButton = this.moreItemsDownEntity.FindComponentInChildren<PressableButton>();

                this.moreItemsUpButton.ButtonReleased += this.MoreItemsUpButton_ButtonReleased;
                this.moreItemsDownButton.ButtonReleased += this.MoreItemsDownButton_ButtonReleased;

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
            this.moreItemsUpButton.ButtonReleased -= this.MoreItemsUpButton_ButtonReleased;
            this.moreItemsDownButton.ButtonReleased -= this.MoreItemsDownButton_ButtonReleased;
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
            var selectedItemIndex = -1;
            bool hasSpaceForAllItems = this.HasSpaceForAllTabItems();

            int i = 0;
            foreach (var child in this.itemsEnumerator)
            {
                TabItem currentItem = this.mappings.ContainsKey(child) ? this.mappings[child] : null;
                if (currentItem == this.selectedItem)
                {
                    // If we don't have enough space for all items, last slot will be reserved
                    // for up/down controls.
                    selectedItemIndex = i;
                    break;
                }

                i++;
            }

            var visibleItemsRange = this.GetVisibleItemsRange();
            bool isInRange = selectedItemIndex >= visibleItemsRange.MinIndex && selectedItemIndex <= visibleItemsRange.MaxIndex;
            this.currentItemPlate.Owner.IsEnabled = Application.Current.IsEditor
                || (this.items.Any() && isInRange);

            if (isInRange)
            {
                this.currentItemChangeAnimation?.TryCancel();

                var animationDuration = TimeSpan.FromMilliseconds(300);
                var toPosition = new Vector3
                {
                    X = this.defaultCurrentItemPlatePosition.X,
                    Y = this.defaultCurrentItemPlatePosition.Y - ((selectedItemIndex - visibleItemsRange.MinIndex) * ButtonConstants.SquareButtonSize),
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

                this.UpdateContent();
                this.SelectedItemChanged?.Invoke(this, new SelectedItemChangedEventArgs(this.selectedItem));
            }
        }

        private void InternalClearItems() =>
            this.buttonsContainer.RemoveAllChildren(child => child != this.moreItemsContainer);

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
            if (!this.IsAttached)
            {
                return;
            }

            bool hasSpaceForAllItems = this.HasSpaceForAllTabItems();
            this.moreItemsContainer.IsEnabled = !hasSpaceForAllItems || Application.Current.IsEditor;

            var visibleItemsRange = this.GetVisibleItemsRange();
            if (visibleItemsRange.MaxIndex - visibleItemsRange.MinIndex <= 0)
            {
                return;
            }

            // We are going to display only items that fit existing space. If current items count
            // fits with available space everything is ok. If not, we have to reserve last
            // item slot to place up/down controls.
            Action<Entity, int> updatePositionFunc = (entity, currentIndexPosition) =>
            {
                var transform = entity.FindComponent<Transform3D>();
                var newPosition = transform.LocalPosition;
                newPosition.Y = -currentIndexPosition * ButtonConstants.SquareButtonSize;
                transform.LocalPosition = newPosition;
            };

            int currentItemPosition = 0;
            foreach (var child in this.itemsEnumerator)
            {
                child.IsEnabled = currentItemPosition >= visibleItemsRange.MinIndex && currentItemPosition <= visibleItemsRange.MaxIndex;
                if (child.IsEnabled)
                {
                    // Update positions for visible items only
                    updatePositionFunc(child, currentItemPosition - visibleItemsRange.MinIndex);
                }

                currentItemPosition++;
            }

            if (!hasSpaceForAllItems)
            {
                updatePositionFunc(this.moreItemsContainer, visibleItemsRange.MaxIndex - visibleItemsRange.MinIndex + 1);
                this.moreItemsDownEntity.FindComponentInChildren<VisuallyEnabledController>().IsVisuallyEnabled = this.HasMoreItemsDown();
                this.moreItemsUpEntity.FindComponentInChildren<VisuallyEnabledController>().IsVisuallyEnabled = this.HasMoreItemsUp();
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

        private int GetMaxNumberOfVisibleItems()
        {
            int maxNumberOfSlots = (int)Math.Floor(this.size.Y / ButtonConstants.SquareButtonSize);

            return this.maxVisibleItems.HasValue
                ? (int)Math.Min(maxNumberOfSlots, this.maxVisibleItems.Value)
                : maxNumberOfSlots;
        }

        private (int MinIndex, int MaxIndex) GetVisibleItemsRange()
        {
            bool hasSpaceForAllItems = this.HasSpaceForAllTabItems();
            int maxVisibleItems = this.GetMaxNumberOfVisibleItems();
            int maxItemPosition = hasSpaceForAllItems ? maxVisibleItems - 1 : maxVisibleItems - 2;
            int maxVisibleItemPosition = maxItemPosition + this.currentSkippedItemsCount;

            return (this.currentSkippedItemsCount, maxVisibleItemPosition);
        }

        private bool HasSpaceForAllTabItems() => this.GetMaxNumberOfVisibleItems() >= (this.items?.Count ?? 0);

        private bool HasMoreItemsUp() => this.currentSkippedItemsCount > 0;

        private bool HasMoreItemsDown()
        {
            var visibleItemsRange = this.GetVisibleItemsRange();
            return visibleItemsRange.MaxIndex < this.items.Count() - 1;
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

        private void MoreItemsDownButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.HasMoreItemsDown())
            {
                this.currentSkippedItemsCount++;
                this.ReorderItems();
                this.UpdateSelectedItem();
            }
        }

        private void MoreItemsUpButton_ButtonReleased(object sender, EventArgs e)
        {
            if (this.HasMoreItemsUp())
            {
                this.currentSkippedItemsCount--;
                this.ReorderItems();
                this.UpdateSelectedItem();
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
