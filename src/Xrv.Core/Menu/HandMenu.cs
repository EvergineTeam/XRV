// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Evergine.Common.Graphics;
using Evergine.Components.Fonts;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Xrv.Core.Extensions;
using Xrv.Core.UI.Windows;

namespace Xrv.Core.Menu
{
    /// <summary>
    /// Manage the Hand menu behavior.
    /// </summary>
    public class HandMenu : Component
    {
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<MenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private int maxButtonsPerColumn = 4;

        private IWorkAction appearAnimation;
        private IWorkAction extendedAnimation;
        private int numberOfButtons;
        private int numberOfColumns;
        private int numberOfButtonsPerColumn;
        private bool isExtended = false;

        private Vector3 initialManipulationBoxSize;
        private Vector3 initialManipulationBoxOffset;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        private XrvService xrvService = null;

        [BindComponent(isExactType: false, source: BindComponentSource.Scene)]
        private IPalmPanelBehavior palmPanelBehavior = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private WindowTagAlong tagAlong = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu")]
        private Transform3D handMenuTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_back_plate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_front_plate")]
        private Transform3D frontPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_detach")]
        private Transform3D detachButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_detach")]
        private ToggleButton detachButtonToggle = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_follow")]
        private Transform3D followButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_follow")]
        private ToggleButton followButtonToggle = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        private Transform3D textTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        private Text3DMesh text3DMesh = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private SimpleManipulationHandler manipulationHandler = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private BoxCollider3D manipulationCollider = null;

        [BindEntity(source: BindEntitySource.Scene, tag: "PART_hand_menu_buttons_container")]
        private Entity buttonsContainer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandMenu"/> class.
        /// </summary>
        public HandMenu()
        {
            this.buttonDescriptions = new ObservableCollection<MenuButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        /// <summary>
        /// Gets or sets number of buttons per column. When a column can't hold more buttons,
        /// a new column will be added.
        /// </summary>
        public int ButtonsPerColumn
        {
            get => this.maxButtonsPerColumn;

            set
            {
                if (this.maxButtonsPerColumn != value && value > 0)
                {
                    this.maxButtonsPerColumn = value;
                    this.ReorderButtons();
                }
            }
        }

        /// <summary>
        /// Gets button descriptions.
        /// </summary>
        public IList<MenuButtonDescription> ButtonDescriptions { get => this.buttonDescriptions; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.InternalAddButtons(this.buttonDescriptions); // We can have items added before this component has been attached
                this.buttonDescriptions.CollectionChanged += this.ButtonDefinitions_CollectionChanged;
                this.initialManipulationBoxSize = this.manipulationCollider.Size;
                this.initialManipulationBoxOffset = this.manipulationCollider.Offset;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            this.buttonDescriptions.Clear();
            this.buttonDescriptions.CollectionChanged -= this.ButtonDefinitions_CollectionChanged;
            this.manipulationCollider.Size = this.initialManipulationBoxSize;
            this.manipulationCollider.Offset = this.initialManipulationBoxOffset;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.palmPanelBehavior != null)
            {
                this.palmPanelBehavior.PalmUpChanged += this.PalmPanelBehavior_PalmUpChanged;
            }

            this.detachButtonToggle.Toggled += this.DetachButtonToggle_Toggled;
            this.followButtonToggle.Toggled += this.FollowButtonToggle_Toggled;

            this.followButtonTransform.Owner.IsEnabled = false;
            this.text3DMesh.Owner.IsEnabled = false;
            this.UpdateFollowBehavior(false);

            this.ReorderButtons();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.palmPanelBehavior != null)
            {
                this.palmPanelBehavior.PalmUpChanged -= this.PalmPanelBehavior_PalmUpChanged;
            }

            this.detachButtonToggle.Toggled -= this.DetachButtonToggle_Toggled;
            this.followButtonToggle.Toggled -= this.FollowButtonToggle_Toggled;
        }

        private void PalmPanelBehavior_PalmUpChanged(object sender, bool palmUp)
        {
            this.AppearAnimation(palmUp);
        }

        private void ButtonDefinitions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.InternalAddButtons(args.NewItems.OfType<MenuButtonDescription>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.InternalRemoveButtons(args.OldItems.OfType<MenuButtonDescription>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.InternalClearButtons();
                    break;
            }

            this.ReorderButtons();
        }

        private void InternalAddButtons(IEnumerable<MenuButtonDescription> buttons)
        {
            var buttonsFactory = new MenuButtonFactory(this.xrvService, this.assetsService);

            foreach (var definition in buttons)
            {
                var buttonInstance = buttonsFactory.CreateInstance(definition);
                Workarounds.MrtkRotateButton(buttonInstance);
                this.buttonsContainer.AddChild(buttonInstance);
                this.instantiatedButtons.Add(definition.Id, buttonInstance);
            }
        }

        private void InternalRemoveButtons(IEnumerable<MenuButtonDescription> buttons)
        {
            var entityManager = this.Managers.EntityManager;

            foreach (var definition in buttons)
            {
                if (this.instantiatedButtons.ContainsKey(definition.Id))
                {
                    var buttonToRemove = this.instantiatedButtons[definition.Id];
                    entityManager.Remove(buttonToRemove);
                    this.instantiatedButtons.Remove(definition.Id);
                }
            }
        }

        private void InternalClearButtons()
        {
            this.instantiatedButtons.Clear();
            this.buttonsContainer.RemoveAllChildren();
        }

        private void ReorderButtons()
        {
            this.numberOfButtons = this.buttonDescriptions.Count;
            this.numberOfColumns = (int)Math.Ceiling(this.numberOfButtons / (float)this.maxButtonsPerColumn);
            this.numberOfButtonsPerColumn = this.numberOfButtons < this.maxButtonsPerColumn ? Math.Max(this.numberOfButtons, 4) : this.maxButtonsPerColumn;

            // Resize back plate
            this.backPlateTransform.LocalScale = new Vector3(this.numberOfColumns, 1, 1);

            // Resize front plate
            this.frontPlateTransform.LocalScale = new Vector3(this.numberOfColumns, this.numberOfButtonsPerColumn, 1);

            // Resize manipulation collider
            this.manipulationCollider.Size = new Vector3(
                this.initialManipulationBoxSize.X * this.numberOfButtonsPerColumn,
                ButtonWidth * (this.numberOfColumns + 1),
                this.initialManipulationBoxSize.Z);
            this.manipulationCollider.Offset = new Vector3(
                this.initialManipulationBoxOffset.X * this.numberOfButtonsPerColumn,
                -ButtonWidth / 2 * (this.numberOfColumns - 1),
                this.initialManipulationBoxOffset.Z);

            // Add buttons
            int i = 0;
            foreach (var button in this.instantiatedButtons.Values.Reverse())
            {
                var buttonTransform = button.FindComponent<Transform3D>();
                buttonTransform.LocalPosition = new Vector3(ButtonWidthOverTwo + (ButtonWidth * (i / this.numberOfButtonsPerColumn)), -ButtonWidthOverTwo - ((i % this.numberOfButtonsPerColumn) * ButtonWidth), 0);
                i++;
            }
        }

        private void AppearAnimation(bool show)
        {
            float start = show ? 0 : 1;
            float end = show ? 1 : 0;

            this.appearAnimation?.Cancel();
            this.appearAnimation = new ActionWorkAction(() =>
            {
                this.handMenuTransform.LocalRotation = show ? new Vector3(0, MathHelper.PiOver2, 0) : new Vector3(0, -MathHelper.Pi, 0);
                if (show)
                {
                    this.handMenuTransform.Owner.IsEnabled = true;
                }
            })
            .ContinueWith(
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, (f) =>
                {
                    this.handMenuTransform.LocalRotation = Vector3.Lerp(new Vector3(0, -MathHelper.PiOver2, 0), new Vector3(0, -MathHelper.Pi, 0), f);
                }))
            .ContinueWith(
                new ActionWorkAction(() =>
                {
                    if (!show)
                    {
                        this.handMenuTransform.Owner.IsEnabled = false;
                    }
                }));
            this.appearAnimation.Run();
        }

        private void ExtendedAnimation(bool extended)
        {
            if (this.isExtended == extended)
            {
                return;
            }

            this.isExtended = extended;

            float start = extended ? 0 : 1;
            float end = extended ? 1 : 0;

            this.extendedAnimation?.Cancel();
            this.extendedAnimation = new ActionWorkAction(() =>
            {
                if (end == 1)
                {
                    this.text3DMesh.Owner.IsEnabled = true;
                    this.followButtonTransform.Owner.IsEnabled = true;
                    this.palmPanelBehavior.Owner.IsEnabled = false;
                }
            })
            .ContinueWith(
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, (f) =>
                {
                    // Front and back plates animation
                    this.frontPlateTransform.Scale = Vector3.Lerp(new Vector3(this.numberOfColumns, this.numberOfButtonsPerColumn, 1), new Vector3(this.numberOfButtonsPerColumn, this.numberOfColumns, 1), f);
                    this.backPlateTransform.Scale = Vector3.Lerp(new Vector3(this.numberOfColumns, 1, 1), new Vector3(this.numberOfButtonsPerColumn, 1, 1), f);

                    // Header animation
                    this.detachButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(ButtonWidthOverTwo, 0, 0.003f), new Vector3(ButtonWidthOverTwo + (ButtonWidth * (this.numberOfButtonsPerColumn - 1)), 0, 0.003f), f);
                    this.followButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidthOverTwo, 0, 0.003f), new Vector3(-ButtonWidthOverTwo + (ButtonWidth * (this.numberOfButtonsPerColumn - 1)), 0, 0.003f), f);

                    this.textTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidth * 2, 0, 0.003f), new Vector3(0.015f, 0, 0.003f), f);
                    this.text3DMesh.Color = Color.Lerp(Color.Transparent, Color.White, f);

                    // Buttons animation
                    int i = 0;
                    foreach (var button in this.instantiatedButtons.Values.Reverse())
                    {
                        var buttonTransform = button.FindComponent<Transform3D>();
                        buttonTransform.LocalPosition = Vector3.Lerp(
                            new Vector3(ButtonWidthOverTwo + (ButtonWidth * (i / this.numberOfButtonsPerColumn)), -ButtonWidthOverTwo - ((i % this.numberOfButtonsPerColumn) * ButtonWidth), 0),
                            new Vector3(ButtonWidthOverTwo + ((i % this.numberOfButtonsPerColumn) * ButtonWidth), -ButtonWidthOverTwo - (ButtonWidth * (i / this.numberOfButtonsPerColumn)), 0),
                            f);

                        i++;
                    }
                }))
            .ContinueWith(
                new ActionWorkAction(() =>
                {
                    if (end == 0)
                    {
                        this.text3DMesh.Owner.IsEnabled = false;
                        this.followButtonTransform.Owner.IsEnabled = false;
                        this.palmPanelBehavior.Owner.IsEnabled = true;
                    }
                }));
            this.extendedAnimation.Run();
        }

        private void DetachButtonToggle_Toggled(object sender, EventArgs e)
        {
            // Reset hand menu local position, as user may have moved it when detached
            if (!this.detachButtonToggle.IsOn)
            {
                this.handMenuTransform.LocalPosition = Vector3.Zero;
            }

            this.UpdateFollowBehavior(false);
            this.ExtendedAnimation(this.detachButtonToggle.IsOn);
        }

        private void FollowButtonToggle_Toggled(object sender, EventArgs e) =>
            this.UpdateFollowBehavior(this.followButtonToggle.IsOn);

        private void UpdateFollowBehavior(bool followEnabled)
        {
            this.tagAlong.IsEnabled = followEnabled;
            this.manipulationHandler.IsEnabled = !followEnabled;
            Workarounds.ChangeToggleButtonState(this.followButtonToggle.Owner, followEnabled);
        }

        //// -- Begin Debug area --

        ////[BindService]
        ////protected GraphicsPresenter graphicsPresenter;

        ////protected override void Update(TimeSpan gameTime)
        ////{
        ////    KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

        ////    if (keyboardDispatcher?.ReadKeyState(Keys.K) == ButtonState.Pressing)
        ////    {
        ////        this.ExtendedAnimation(true);
        ////    }
        ////    else if (keyboardDispatcher?.ReadKeyState(Keys.J) == ButtonState.Pressing)
        ////    {
        ////        this.ExtendedAnimation(false);
        ////    }

        ////    if (keyboardDispatcher?.ReadKeyState(Keys.O) == ButtonState.Pressing)
        ////    {
        ////        this.AppearAnimation(true);
        ////    }
        ////    else if (keyboardDispatcher?.ReadKeyState(Keys.P) == ButtonState.Pressing)
        ////    {
        ////        this.AppearAnimation(false);
        ////    }

        ////    if (keyboardDispatcher?.ReadKeyState(Keys.I) == ButtonState.Pressing)
        ////    {
        ////        this.AddButton();
        ////    }
        ////}

        ////private void AddButton()
        ////{
        ////    this.ButtonDescriptions.Add(new MenuButtonDescription
        ////    {
        ////        IsToggle = false,
        ////        TextOn = this.buttonDescriptions.Count.ToString(),
        ////        IconOn = CoreResourcesIDs.Materials.Icons.Help,
        ////    });
        ////}

        //// -- End Debug area --
    }
}
