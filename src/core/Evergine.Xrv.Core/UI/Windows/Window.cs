// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Buttons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Component to work with windows.
    /// </summary>
    public class Window : Component, IButtonsOrganizerCallback
    {
        /// <summary>
        /// XRV service instance.
        /// </summary>
        [BindService]
        protected XrvService xrvService = null;

        private readonly ActionButtonsOrganizer buttonsOrganizer;
        private Entity logoEntity = null;
        private bool allowPin = true;
        private bool enableManipulation = true;
        private bool followEnabled = false;
        private bool showCloseButton = true;
        private ToggleButton followButtonToggle;
        private ToggleButton moreActionsButtonToggle;
        private PressableButton closeButtonPressable;
        private MoreActionsPanelBehavior moreActionsBehavior;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        private LocalizationService localizationService = null;

        [BindComponent(source: BindComponentSource.Owner, isExactType: false)]
        private BaseWindowConfigurator configurator = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent(isRequired: false)]
        private SimpleManipulationHandler simpleManipulationHandler = null;

        [BindComponent]
        private WindowTagAlong windowTagAlong = null;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_window_title_buttons_container")]
        private Entity actionsBarContainer = null;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_window_title_buttons_moreActions")]
        private Entity moreActionsContainer = null;

        [BindEntity(source: BindEntitySource.Children, tag: "PART_window_title_buttons_moreActions_background")]
        private Entity moreActionsBackground = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
        {
            this.buttonsOrganizer = new ActionButtonsOrganizer(this);
        }

        /// <summary>
        /// Raised before window is opened.
        /// </summary>
        public event EventHandler Opening;

        /// <summary>
        /// Raised after window is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Raised before window is closed.
        /// </summary>
        public event EventHandler Closing;

        /// <summary>
        /// Raised after window is closed.
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        /// Raised when any action button from <see cref="ExtraActionButtons"/> has been pressed.
        /// </summary>
        public event EventHandler<ActionButtonPressedEventArgs> ActionButtonPressed;

        /// <summary>
        /// Gets window configurator.
        /// </summary>
        [IgnoreEvergine]
        public BaseWindowConfigurator Configurator { get => this.configurator; }

        /// <summary>
        /// Gets a value indicating whether window is opened.
        /// </summary>
        public bool IsOpened { get => this.Owner.IsEnabled; }

        /// <summary>
        /// Gets or sets a value indicating whether window pin is enabled or not.
        /// </summary>
        public bool AllowPin
        {
            get => this.allowPin;

            set
            {
                if (this.allowPin != value)
                {
                    this.allowPin = value;
                    this.UpdateAllowPin();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether window manipulation is enabled or not.
        /// </summary>
        public bool EnableManipulation
        {
            get => this.enableManipulation;

            set
            {
                if (this.enableManipulation != value)
                {
                    this.enableManipulation = value;
                    this.UpdateEnableManipulation();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether window should be placed in front
        /// of user when it's opened.
        /// </summary>
        public bool PlaceInFrontOfUserWhenOpened { get; set; } = true;

        /// <summary>
        /// Gets or sets distance key to be used from <see cref="Distances"/> to get distance
        /// where window should be displayed to the user when is opened.
        /// </summary>
        [IgnoreEvergine]
        public string DistanceKey { get; set; }

        /// <summary>
        /// Gets list of button descriptions to be added as part of window extra actions. Each one of this descriptions
        /// will be rendered as a separate button on window's action bar. Depending on <see cref="AvailableActionSlots"/> and
        /// <see cref="ButtonDescription.Order"/>, some buttons will be placed in action bar or in a context menu that will be
        /// displayed after tapping in a dots (more actions) button.
        /// </summary>
        [IgnoreEvergine]
        public IList<ButtonDescription> ExtraActionButtons { get => this.buttonsOrganizer.ExtraButtons; }

        /// <summary>
        /// Gets or sets number of button slots available in action bar. If number of <see cref="ExtraActionButtons"/> is bigger than this
        /// number of slots, some of the buttons will be displayed in a context menu, depending on its <see cref="ButtonDescription.Order"/>.
        /// </summary>
        public int AvailableActionSlots
        {
            get => this.buttonsOrganizer.AvailableActionSlots;
            set => this.buttonsOrganizer.AvailableActionSlots = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether top right close window button should be displayed or not.
        /// </summary>
        public bool ShowCloseButton
        {
            get => this.showCloseButton;

            set
            {
                if (this.showCloseButton != value)
                {
                    this.showCloseButton = value;
                    this.buttonsOrganizer.IncludeCloseButton = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets more actions button placement.
        /// </summary>
        public MoreActionsButtonPlacement MoreActionsPlacement
        {
            get => this.buttonsOrganizer.MoreActionsPlacement;

            set => this.buttonsOrganizer.MoreActionsPlacement = value;
        }

        /// <summary>
        /// Gets or sets more actions panel behavior.
        /// </summary>
        public MoreActionsPanelBehavior MoreActionsBehavior
        {
            get => this.moreActionsBehavior;

            set => this.moreActionsBehavior = value;
        }

        [IgnoreEvergine]
        internal ActionButtonsOrganizer ButtonsOrganizer { get => this.buttonsOrganizer; }

        /// <summary>
        /// Opens the window, if it's not already opened.
        /// </summary>
        public void Open()
        {
            if (!this.IsOpened)
            {
                this.Opening?.Invoke(this, EventArgs.Empty);
                this.Owner.IsEnabled = true;
                this.Opened?.Invoke(this, EventArgs.Empty);
            }

            if (this.PlaceInFrontOfUserWhenOpened)
            {
                this.PlaceInFrontOfUser();
            }
        }

        /// <summary>
        /// Closes the window, if it's not already closed.
        /// </summary>
        public void Close()
        {
            if (this.IsOpened)
            {
                this.Closing?.Invoke(this, EventArgs.Empty);
                this.UpdateFollowBehavior(false);
                this.Owner.IsEnabled = false;
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets button entity associated to a given description. Description should be part of
        /// <see cref="ExtraActionButtons"/> collection.
        /// </summary>
        /// <param name="description">Target description.</param>
        /// <returns>Button entity if description is found; null otherwise.</returns>
        public Entity GetActionButtonEntity(ButtonDescription description) => this.buttonsOrganizer[description];

        /// <inheritdoc/>
        void IButtonsOrganizerCallback.BeforeUpdatingLayout()
        {
            this.UnsubscribeBuiltInButtonEvents();
            this.UnsubscribeActionButtonEvents();
        }

        /// <inheritdoc/>
        void IButtonsOrganizerCallback.AfterUpdatingLayout()
        {
            this.SubscribeBuiltInButtonEvents();
            this.SubscribeActionButtonEvents();
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                var buttonsFactory = new ButtonFactory(this.assetsService);
                this.buttonsOrganizer.Initialize(buttonsFactory.CreateInstance, this.localizationService);
                this.logoEntity = this.Owner.FindChildrenByTag("PART_window_logo", isRecursive: true).First();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateButtonsLayout();
            this.UpdateAllowPin();
            this.UpdateEnableManipulation();
            this.UpdateFollowBehavior(false);

            this.buttonsOrganizer.OrganizationUpdated += this.ButtonsOrganizer_OrganizationUpdated;

            if (!Application.Current.IsEditor)
            {
                this.moreActionsContainer.IsEnabled = false;
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.buttonsOrganizer.OrganizationUpdated -= this.ButtonsOrganizer_OrganizationUpdated;

            if (this.moreActionsButtonToggle?.Owner != null)
            {
                Workarounds.ChangeToggleButtonState(this.moreActionsButtonToggle, false);
            }
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.UnsubscribeBuiltInButtonEvents();
            this.UnsubscribeActionButtonEvents();
        }

        /// <summary>
        /// Gets distance where window will be displayed when opened.
        /// </summary>
        /// <returns>Distance.</returns>
        protected virtual float GetOpenDistance()
        {
            var distances = this.xrvService.WindowsSystem.Distances;
            return distances.GetDistanceOrAlternative(this.DistanceKey, Distances.MediumKey);
        }

        /// <summary>
        /// Changes window logo visibility.
        /// </summary>
        /// <param name="visible">True to make it visible; false otherwise.</param>
        protected void UpdateLogoVisibility(bool visible) => this.logoEntity.IsEnabled = visible;

        private void ButtonsOrganizer_OrganizationUpdated(object sender, EventArgs e) => this.UpdateButtonsLayout();

        private void UpdateButtonsLayout()
        {
            // Populate actions button container
            var numberOfActionBarButtons = this.buttonsOrganizer.ActionBarButtons.Count;
            var offset = ButtonConstants.SquareButtonSize * (numberOfActionBarButtons - 1);
            for (int i = 0; i < this.buttonsOrganizer.ActionBarButtons.Count; i++)
            {
                var currentButton = this.buttonsOrganizer.ActionBarButtons[i];
                var transform = currentButton.FindComponent<Transform3D>();
                var buttonPosition = transform.LocalPosition;
                buttonPosition.X = -offset + (i * ButtonConstants.SquareButtonSize) - 0.02f;
                transform.LocalPosition = buttonPosition;

                if (!this.actionsBarContainer.ChildEntities.Contains(currentButton))
                {
                    this.actionsBarContainer.AddChild(currentButton);
                }
            }

            this.actionsBarContainer.RemoveAllChildren(
                child => !this.buttonsOrganizer.ActionBarButtons.Contains(child) && child != this.moreActionsContainer);

            // Populate more actions container
            var numberOfMoreActionButtons = this.buttonsOrganizer.MoreActionButtons.Count;
            var initialOffset = (ButtonConstants.SquareButtonSize / 2) * (numberOfMoreActionButtons - 1);
            for (int i = 0; i < numberOfMoreActionButtons; i++)
            {
                var currentButton = this.buttonsOrganizer.MoreActionButtons[i];
                var transform = currentButton.FindComponent<Transform3D>();
                var buttonPosition = transform.LocalPosition;
                buttonPosition.Y = initialOffset - (i * ButtonConstants.SquareButtonSize);
                transform.LocalPosition = buttonPosition;

                if (!this.moreActionsContainer.ChildEntities.Contains(currentButton))
                {
                    this.moreActionsContainer.AddChild(currentButton);
                }
            }

            this.moreActionsContainer.RemoveAllChildren(
                child => !this.buttonsOrganizer.MoreActionButtons.Contains(child) && child != this.moreActionsBackground);

            // Calculate more actions panel size and position
            var moreActionsContainerTransform = this.moreActionsContainer.FindComponent<Transform3D>();
            var localPosition = moreActionsContainerTransform.LocalPosition;
            localPosition.X = -ButtonConstants.SquareButtonSize * (numberOfActionBarButtons - 1);
            localPosition.Y = -ButtonConstants.SquareButtonSize - ((ButtonConstants.SquareButtonSize / 2) * (numberOfMoreActionButtons - 1));
            moreActionsContainerTransform.LocalPosition = localPosition;
            var moreActionsBackgroundTransform = this.moreActionsBackground.FindComponent<Transform3D>();
            var localScale = moreActionsBackgroundTransform.LocalScale;
            localScale.Y = ButtonConstants.SquareButtonSize * numberOfMoreActionButtons;
            moreActionsBackgroundTransform.LocalScale = localScale;

            // Ensure buttons are properly initialized (toggle states)
            this.UpdateFollowButtonState();
        }

        private void SubscribeActionButtonEvents()
        {
            this.InternalHandleActionButtonSubscriptions(this.buttonsOrganizer.ActionBarButtons, true);
            this.InternalHandleActionButtonSubscriptions(this.buttonsOrganizer.MoreActionButtons, true);
        }

        private void UnsubscribeActionButtonEvents()
        {
            this.InternalHandleActionButtonSubscriptions(this.buttonsOrganizer.ActionBarButtons, false);
            this.InternalHandleActionButtonSubscriptions(this.buttonsOrganizer.MoreActionButtons, false);
        }

        private void InternalHandleActionButtonSubscriptions(IEnumerable<Entity> buttonEntities, bool subscribe)
        {
            foreach (var button in buttonEntities)
            {
                if (button.FindComponentInChildren<PressableButton>() is PressableButton pressable)
                {
                    if (subscribe)
                    {
                        pressable.ButtonReleased += this.InternalActionButton_Pressed;
                        pressable.ButtonReleased += this.ButtonReleaseHandleForMoreActionsPanelBehavior;
                    }
                    else
                    {
                        pressable.ButtonReleased -= this.InternalActionButton_Pressed;
                        pressable.ButtonReleased -= this.ButtonReleaseHandleForMoreActionsPanelBehavior;
                    }
                }
            }
        }

        private void InternalActionButton_Pressed(object sender, EventArgs args)
        {
            bool toggleValue = true;

            var senderEntity = (sender as Component)?.Owner
                ?.FindComponentInParents<StandardButtonConfigurator>(isExactType: false)
                ?.Owner;
            if (senderEntity?.FindComponentInChildren<ToggleButton>() is ToggleButton toggle)
            {
                senderEntity = toggle.Owner;
                toggleValue = toggle.IsOn;
            }

            var description = this.buttonsOrganizer.GetButtonDescriptionByEntity(senderEntity);
            if (description != null)
            {
                this.ActionButtonPressed?.Invoke(this, new ActionButtonPressedEventArgs(description, toggleValue));
            }
        }

        private void ButtonReleaseHandleForMoreActionsPanelBehavior(object sender, EventArgs e)
        {
            var senderEntity = (sender as Component)?.Owner;
            if (senderEntity?.FindComponentInParents<ToggleButton>() is ToggleButton toggle)
            {
                senderEntity = toggle.Owner;
            }

            if (senderEntity == null)
            {
                return;
            }

            bool isMoreActionsButtonSender = this.buttonsOrganizer.MoreActionsButtonEntity != senderEntity;
            if (!isMoreActionsButtonSender)
            {
                return;
            }

            if (this.moreActionsButtonToggle != null &&
                this.moreActionsBehavior == MoreActionsPanelBehavior.HideAutomatically)
            {
                Workarounds.ChangeToggleButtonState(this.moreActionsButtonToggle, false);
            }
        }

        private void SubscribeBuiltInButtonEvents()
        {
            if (this.buttonsOrganizer.MoreActionsButtonEntity != null)
            {
                this.moreActionsButtonToggle = this.buttonsOrganizer.MoreActionsButtonEntity.FindComponentInChildren<ToggleButton>();
                this.moreActionsButtonToggle.Toggled += this.MoreButtonToggle_Toggled;
            }
            else
            {
                this.moreActionsButtonToggle = null;
            }

            this.followButtonToggle = this.buttonsOrganizer.FollowButtonEntity.FindComponentInChildren<ToggleButton>();
            this.followButtonToggle.Toggled += this.FollowButtonToggled;

            this.closeButtonPressable = this.buttonsOrganizer.CloseButtonEntity.FindComponentInChildren<PressableButton>();
            this.closeButtonPressable.ButtonReleased += this.CloseButtonReleased;
        }

        private void UnsubscribeBuiltInButtonEvents()
        {
            if (this.moreActionsButtonToggle != null)
            {
                this.moreActionsButtonToggle.Toggled -= this.MoreButtonToggle_Toggled;
            }

            if (this.followButtonToggle != null)
            {
                this.followButtonToggle.Toggled -= this.FollowButtonToggled;
            }

            if (this.closeButtonPressable != null)
            {
                this.closeButtonPressable.ButtonReleased -= this.CloseButtonReleased;
            }
        }

        private void UpdateAllowPin()
        {
            if (this.IsAttached && this.buttonsOrganizer.FollowButtonEntity != null)
            {
                this.buttonsOrganizer.FollowButtonEntity.IsEnabled = this.allowPin;
            }
        }

        private void CloseButtonReleased(object sender, EventArgs e) => this.Close();

        private void FollowButtonToggled(object sender, EventArgs args)
        {
            if (sender is ToggleButton toggle)
            {
                this.UpdateFollowBehavior(toggle.IsOn);
            }
        }

        private void PlaceInFrontOfUser()
        {
            var camera = this.Managers.RenderManager.ActiveCamera3D;
            var distance = this.GetOpenDistance();
            var position = camera.Transform.Position + (camera.Transform.WorldTransform.Forward * distance);
            this.transform.Position = position;
            this.transform.LookAt(camera.Transform.Position, Vector3.Up, false);
        }

        private void UpdateFollowBehavior(bool followEnabled)
        {
            this.followEnabled = followEnabled;
            this.EnableManipulation = !followEnabled;
            this.windowTagAlong.IsEnabled = followEnabled;
            this.UpdateFollowButtonState();
        }

        private void UpdateFollowButtonState()
        {
            if (this.followButtonToggle?.Owner != null)
            {
                Workarounds.ChangeToggleButtonState(this.followButtonToggle, this.followEnabled);
            }
        }

        private void UpdateEnableManipulation()
        {
            if (this.simpleManipulationHandler != null)
            {
                this.simpleManipulationHandler.IsEnabled = this.enableManipulation;
            }
        }

        private void MoreButtonToggle_Toggled(object sender, EventArgs e) =>
            this.moreActionsContainer.IsEnabled = this.moreActionsButtonToggle?.IsOn ?? false;
    }
}
