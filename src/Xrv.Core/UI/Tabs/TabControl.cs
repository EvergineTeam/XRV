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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Xrv.Core.Extensions;

namespace Xrv.Core.UI.Tabs
{
    public class TabControl : Component
    {
        private const float ButtonHeight = 0.032f;
        private readonly ObservableCollection<TabItem> items;
        private readonly Dictionary<Entity, TabItem> mappings;

        private Vector2 size;
        private TabItem selectedItem;
        private Entity contentsContainer;

        private Vector3 defaultCurrentItemPlatePosition;
        private IWorkAction currentItemChangeAnimation;
        private Color inactiveItemTextColor;
        private Color activeItemTextColor;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_front_plate")]
        protected PlaneMesh frontPlate;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_current_item_plate")]
        protected PlaneMesh currentItemPlate;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_current_item_plate")]
        protected Transform3D currentItemPlateTransform;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_tab_control_buttons_container")]
        protected Transform3D buttonsContainerTransform;

        protected Entity buttonsContainer;

        public TabControl()
        {
            this.items = new ObservableCollection<TabItem>();
            this.mappings = new Dictionary<Entity, TabItem>();
            this.size = new Vector2(0.3f, 0.2f);
        }

        public Vector2 Size
        {
            get => this.size;
            set
            {
                this.size = value;
                if (this.IsAttached)
                {
                    this.UpdateFrontPlateSize();
                }
            }
        }

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

        public bool DestroyContentOnTabChange { get; set; }

        public IList<TabItem> Items { get => this.items; }

        public static TabControlBuilder Builder { get; internal set; }

        public event EventHandler<SelectedItemChangedEventArgs> SelectedItemChanged;

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.items.CollectionChanged += this.Items_CollectionChanged;
                this.buttonsContainer = this.Owner.FindChildrenByTag("PART_tab_control_buttons_container", isRecursive: true).First();
                this.contentsContainer = this.Owner.FindChildrenByTag("PART_tab_control_current_item_contents", isRecursive: true).First();
                this.InternalAddItems(this.items); // We can have items added before this component has been attached
                this.selectedItem = this.items.FirstOrDefault();
            }

            return attached;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateFrontPlateSize();
            this.ReorderItems();
            this.UpdateItemsTextColor();
            this.UpdateSelectedItem();
        }

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
            currentItemPosition.X = ((this.currentItemPlate.Width  + this.size.X) * -0.5f) + 0.015f; //-this.size.X * 0.43f;
            currentItemPosition.Y = this.size.Y / 2 - this.currentItemPlate.Height / 2;
            this.currentItemPlateTransform.LocalPosition = currentItemPosition;
            this.defaultCurrentItemPlatePosition = currentItemPosition;

            var buttonsPosition = this.buttonsContainerTransform.LocalPosition;
            buttonsPosition.X = currentItemPosition.X - 0.025f;
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
                Workarounds.MrtkRotateButton(buttonInstance);
                var transform = buttonInstance.FindComponent<Transform3D>();
                var position = transform.LocalPosition;
                position.Y = this.buttonsContainer.ChildEntities.Count() * ButtonHeight;
                transform.LocalPosition = position;

                var pressableButton = buttonInstance.FindComponentInChildren<PressableButton>();
                pressableButton.ButtonReleased += this.PressableButton_ButtonReleased;

                this.buttonsContainer.AddChild(buttonInstance);
                this.mappings.Add(buttonInstance, item);
            }

            this.UpdateItemsTextColor();
        }

        private void InternalRemoveItems(IEnumerable<TabItem> items)
        {
            bool currentItemRemoved = false;
            int currentItemIndex = -1;

            for (int i = 0; i < this.mappings.Count(); i++)
            {
                Entity owner = this.mappings.ElementAt(i).Key;
                TabItem item = this.mappings.ElementAt(i).Value;
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

                var pressableButton = owner.FindComponentInChildren<PressableButton>();
                pressableButton.ButtonReleased -= this.PressableButton_ButtonReleased;
            }

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

            for (int i = 0; i < this.buttonsContainer.ChildEntities.Count(); i++)
            {
                Entity child = this.buttonsContainer.ChildEntities.ElementAt(i);
                TabItem currentItem = this.mappings.ContainsKey(child) ? this.mappings[child] : null;
                if (currentItem == this.selectedItem)
                {
                    itemIndex = i;
                    break;
                }
            }

            if (itemIndex != -1)
            {
                this.currentItemChangeAnimation?.Cancel();

                var animationDuration = TimeSpan.FromMilliseconds(300);
                var toPosition = new Vector3
                {
                    X = this.defaultCurrentItemPlatePosition.X,
                    Y = this.defaultCurrentItemPlatePosition.Y - itemIndex * ButtonHeight,
                    Z = this.defaultCurrentItemPlatePosition.Z,
                };

                this.currentItemChangeAnimation = new Vector3AnimationWorkAction(
                    this.Owner,
                    this.currentItemPlateTransform.LocalPosition,
                    toPosition,
                    animationDuration,
                    EaseFunction.CubicOutEase, @value =>
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

            var content = selectedItem.Contents?.Invoke();
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
                for (int i = 0; i < this.buttonsContainer.ChildEntities.Count(); i++)
                {
                    var child = this.buttonsContainer.ChildEntities.ElementAt(i);
                    var transform = child.FindComponent<Transform3D>();
                    var position = transform.LocalPosition;
                    position.Y = -i * ButtonHeight;
                    transform.LocalPosition = position;
                }

                this.currentItemPlate.Owner.IsEnabled = this.items.Any();
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
                    configurator.PrimaryColor = isSelected ? this.activeItemTextColor : this.inactiveItemTextColor;
                }
            }
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
    }
}
