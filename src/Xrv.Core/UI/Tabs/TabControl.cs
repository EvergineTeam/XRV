using Evergine.Common.Attributes;
using Evergine.Components.Graphics3D;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
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

        private Vector2 size;
        private TabItem selectedItem;
        private Entity content;
        private Entity contentsContainer;

        private Vector3 defaultCurrentItemPlatePosition;
        private IWorkAction currentItemChangeAnimation;

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
        public Entity Content
        {
            get => this.content;
            set
            {
                this.content = value;
                if (this.IsAttached)
                {
                    this.UpdateContent();
                }
            }
        }

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
                this.ReorderItems();
            }

            return attached;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateFrontPlateSize();
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
            currentItemPosition.X = -this.size.X / 2;
            currentItemPosition.Y = this.size.Y / 2 - this.currentItemPlate.Height / 2;
            this.currentItemPlateTransform.LocalPosition = currentItemPosition;
            this.defaultCurrentItemPlatePosition = currentItemPosition;

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
                Workarounds.MrtkRotateButton(buttonInstance);
                var transform = buttonInstance.FindComponent<Transform3D>();
                var position = transform.LocalPosition;
                position.Y = this.buttonsContainer.ChildEntities.Count() * ButtonHeight;
                transform.LocalPosition = position;

                this.buttonsContainer.AddChild(buttonInstance);
            }
        }

        private void InternalRemoveItems(IEnumerable<TabItem> items)
        {
            var associations = this.buttonsContainer.FindComponentsInChildren<TabItemAssociation>();
            foreach (var association in associations)
            {
                if (items.Contains(association.Item))
                {
                    this.Managers.EntityManager.Remove(association.Owner);
                }
            }
        }

        private void UpdateSelectedItem()
        {
            var itemAssociations = this.Owner.FindComponentsInChildren<TabItemAssociation>();
            var itemIndex = -1;

            for (int i = 0; i < itemAssociations.Count(); i++)
            {
                var association = itemAssociations.ElementAt(i);
                association.IsSelected = association.Item == this.selectedItem;
                if (association.IsSelected)
                {
                    itemIndex = i;
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

            this.SelectedItemChanged?.Invoke(this, new SelectedItemChangedEventArgs(this.selectedItem));
        }

        private void InternalClearItems() => this.buttonsContainer.RemoveAllChildren();

        private void UpdateContent()
        {
            this.contentsContainer.RemoveAllChildren();
            if (this.content != null)
            {
                this.contentsContainer.AddChild(this.content);
            }
        }

        private void ReorderItems()
        {
            for (int i = 0; i < this.buttonsContainer.ChildEntities.Count(); i++)
            {
                var child = this.buttonsContainer.ChildEntities.ElementAt(i);
                var transform = child.FindComponent<Transform3D>();
                var position = transform.LocalPosition;
                position.Y = -i * ButtonHeight;
                transform.LocalPosition = position;
            }
        }
    }
}
