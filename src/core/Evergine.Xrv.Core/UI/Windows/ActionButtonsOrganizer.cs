// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Evergine.Framework;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.UI.Windows
{
    internal class ActionButtonsOrganizer
    {
        private const string DefaultButtonEntityName = "action-button";

        private readonly ObservableCollection<ButtonDescription> extraButtonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedActionBarButtons;
        private readonly Dictionary<Guid, Entity> instantiatedMoreActionButtons;
        private readonly List<Entity> actionBarButtons;
        private readonly ReadOnlyCollection<Entity> readOnlyActionBarButtons;
        private readonly List<Entity> moreActionsButtons;
        private readonly ReadOnlyCollection<Entity> readOnlyMoreActionsButtons;
        private readonly IButtonsOrganizerCallback organizerCallback;

        private bool isInitialized;
        private Func<ButtonDescription, Guid, Entity> entityLoader;
        private LocalizationService localization;
        private int availableActionSlots = 3;
        private ButtonDescription closeButtonDescription;
        private ButtonDescription followButtonDescription;
        private ButtonDescription moreActionsDescription;
        private bool includeCloseButton = true;
        private bool includeFollowButton = true;
        private MoreActionsButtonPlacement moreActionsPlacement = MoreActionsButtonPlacement.BeforeFollowAndClose;

        public ActionButtonsOrganizer()
            : this(null)
        {
        }

        public ActionButtonsOrganizer(IButtonsOrganizerCallback organizerCallback)
        {
            this.extraButtonDescriptions = new ObservableCollection<ButtonDescription>();
            this.extraButtonDescriptions.CollectionChanged += this.ExtraButtonDescriptions_CollectionChanged;
            this.instantiatedActionBarButtons = new Dictionary<Guid, Entity>();
            this.instantiatedMoreActionButtons = new Dictionary<Guid, Entity>();
            this.actionBarButtons = new List<Entity>();
            this.readOnlyActionBarButtons = new ReadOnlyCollection<Entity>(this.actionBarButtons);
            this.moreActionsButtons = new List<Entity>();
            this.readOnlyMoreActionsButtons = new ReadOnlyCollection<Entity>(this.moreActionsButtons);
            this.organizerCallback = organizerCallback;
        }

        public event EventHandler OrganizationUpdated;

        public IList<ButtonDescription> ExtraButtons { get => this.extraButtonDescriptions; }

        public ReadOnlyCollection<Entity> ActionBarButtons { get => this.readOnlyActionBarButtons; }

        public ReadOnlyCollection<Entity> MoreActionButtons { get => this.readOnlyMoreActionsButtons; }

        public int AvailableActionSlots
        {
            get => this.availableActionSlots;
            set
            {
                if (this.availableActionSlots != value)
                {
                    this.availableActionSlots = value;
                    this.UpdateOrganization();
                }
            }
        }

        public bool HasMoreActionsButton { get => this.moreActionsDescription != null; }

        public Entity FollowButtonEntity => this.GetButtonEntityByDescription(this.followButtonDescription);

        public Entity CloseButtonEntity => this.GetButtonEntityByDescription(this.closeButtonDescription);

        public Entity MoreActionsButtonEntity => this.GetButtonEntityByDescription(this.moreActionsDescription);

        public bool IncludeCloseButton
        {
            get => this.includeCloseButton;

            set
            {
                if (this.includeCloseButton != value)
                {
                    this.includeCloseButton = value;
                    this.UpdateOrganization();
                }
            }
        }

        public bool IncludeFollowButton
        {
            get => this.includeFollowButton;

            set
            {
                if (this.includeFollowButton != value)
                {
                    this.includeFollowButton = value;
                    this.UpdateOrganization();
                }
            }
        }

        public MoreActionsButtonPlacement MoreActionsPlacement
        {
            get => this.moreActionsPlacement;

            set
            {
                if (this.moreActionsPlacement != value)
                {
                    this.moreActionsPlacement = value;
                    this.UpdateOrganization();
                }
            }
        }

        public Entity this[ButtonDescription description]
        {
            get
            {
                if (this.instantiatedActionBarButtons.ContainsKey(description.Id))
                {
                    return this.instantiatedActionBarButtons[description.Id];
                }

                if (this.instantiatedMoreActionButtons.ContainsKey(description.Id))
                {
                    return this.instantiatedMoreActionButtons[description.Id];
                }

                return null;
            }
        }

        public void Initialize(Func<ButtonDescription, Guid, Entity> entityLoader, LocalizationService localizationService)
        {
            if (this.isInitialized)
            {
                throw new InvalidOperationException("Already initialized");
            }

            this.entityLoader = entityLoader;
            this.localization = localizationService;
            this.closeButtonDescription = new ButtonDescription
            {
                IsToggle = false,
                TextOn = () => this.localization.GetString(() => Resources.Strings.Window_Button_Close),
                IconOn = CoreResourcesIDs.Materials.Icons.close,
                Name = "close",
            };
            this.followButtonDescription = new ButtonDescription
            {
                IsToggle = true,
                TextOff = () => this.localization.GetString(() => Resources.Strings.Window_Button_Follow),
                TextOn = () => this.localization.GetString(() => Resources.Strings.Window_Button_Pin),
                IconOff = CoreResourcesIDs.Materials.Icons.follow,
                IconOn = CoreResourcesIDs.Materials.Icons.pin,
                Name = "follow",
            };

            this.isInitialized = true;
            this.UpdateOrganization();
        }

        public Entity GetButtonEntityByDescription(ButtonDescription description)
        {
            if (description == null)
            {
                return default;
            }

            if (this.instantiatedActionBarButtons.ContainsKey(description.Id))
            {
                return this.instantiatedActionBarButtons[description.Id];
            }

            if (this.instantiatedMoreActionButtons.ContainsKey(description.Id))
            {
                return this.instantiatedMoreActionButtons[description.Id];
            }

            return default;
        }

        public ButtonDescription GetButtonDescriptionByEntity(Entity buttonEntity)
        {
            foreach (var actionBarEntry in this.instantiatedActionBarButtons)
            {
                if (object.ReferenceEquals(actionBarEntry.Value, buttonEntity))
                {
                    return this.extraButtonDescriptions.FirstOrDefault(desc => desc.Id == actionBarEntry.Key);
                }
            }

            foreach (var moreActionsBarEntry in this.instantiatedMoreActionButtons)
            {
                if (object.ReferenceEquals(moreActionsBarEntry.Value, buttonEntity))
                {
                    return this.extraButtonDescriptions.FirstOrDefault(desc => desc.Id == moreActionsBarEntry.Key);
                }
            }

            return null;
        }

        private void ExtraButtonDescriptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args) =>
            this.UpdateOrganization();

        private void UpdateOrganization()
        {
            if (!this.isInitialized)
            {
                return;
            }

            this.organizerCallback?.BeforeUpdatingLayout();
            this.actionBarButtons.Clear();
            this.moreActionsButtons.Clear();
            this.CreateOrDeleteMoreActionsDescriptionIfRequired();

            // If there is a more actions description, its button should be the first in actions bar if
            // we are using BeforeActionButtons approach
            if (this.moreActionsDescription != null
                && this.moreActionsPlacement == MoreActionsButtonPlacement.BeforeActionButtons)
            {
                this.CreateButtonInstanceForContainer(this.moreActionsDescription, true);
            }

            // Then, fill both containers with rest of buttons, except follow and close buttons that should be always
            // at the end of action bar. First calculate real number of available button slots.
            int freeActionBarSlots = this.CalculateActionBarInitialFreeSlots() - (this.moreActionsDescription != null ? 1 : 0);
            foreach (var currentDescription in this.extraButtonDescriptions.OrderBy(description => description.Order))
            {
                this.CreateButtonInstanceForContainer(currentDescription, freeActionBarSlots > 0);
                freeActionBarSlots = Math.Max(0, freeActionBarSlots - 1);
            }

            // If there is a more actions description, its button should be after action buttons and before follow
            // and close buttons if we are using AfterFollowAndClose approach
            if (this.moreActionsDescription != null
                && this.moreActionsPlacement == MoreActionsButtonPlacement.BeforeFollowAndClose)
            {
                this.CreateButtonInstanceForContainer(this.moreActionsDescription, true);
            }

            // Finally, add follow and close buttons
            if (this.includeFollowButton)
            {
                this.CreateButtonInstanceForContainer(this.followButtonDescription, true);
            }
            else
            {
                this.RemoveButtonInstanceForContainer(this.followButtonDescription, true);
            }

            if (this.includeCloseButton)
            {
                this.CreateButtonInstanceForContainer(this.closeButtonDescription, true);
            }
            else
            {
                this.RemoveButtonInstanceForContainer(this.closeButtonDescription, true);
            }

            this.OrganizationUpdated?.Invoke(this, EventArgs.Empty);
            this.organizerCallback?.AfterUpdatingLayout();
        }

        private void CreateButtonInstanceForContainer(ButtonDescription description, bool isActionBarButton)
        {
            if (isActionBarButton)
            {
                // Ensure button has not been marked as destroyed (for example, if we change close button visibility
                // the instantiated button may be removed from windows hierarchy.
                if (this.instantiatedActionBarButtons.ContainsKey(description.Id) &&
                    this.instantiatedActionBarButtons[description.Id].State == AttachableObjectState.Destroyed)
                {
                    this.instantiatedActionBarButtons.Remove(description.Id);
                }

                if (!this.instantiatedActionBarButtons.ContainsKey(description.Id))
                {
                    Entity buttonInstance = this.CreateButtonEntity(
                        description,
                        description.IsToggle ? CoreResourcesIDs.Prefabs.baseToggleButton_weprefab : CoreResourcesIDs.Prefabs.baseButton_weprefab);
                    this.instantiatedActionBarButtons.Add(description.Id, buttonInstance);
                }

                this.instantiatedMoreActionButtons.Remove(description.Id);
                this.actionBarButtons.Add(this.instantiatedActionBarButtons[description.Id]);
            }
            else
            {
                if (this.instantiatedMoreActionButtons.ContainsKey(description.Id) &&
                    this.instantiatedMoreActionButtons[description.Id].State == AttachableObjectState.Destroyed)
                {
                    this.instantiatedMoreActionButtons.Remove(description.Id);
                }

                if (!this.instantiatedMoreActionButtons.ContainsKey(description.Id))
                {
                    Entity buttonInstance = this.CreateButtonEntity(description, CoreResourcesIDs.Prefabs.iconTextButton_weprefab);
                    buttonInstance.FindComponentInChildren<ButtonCursorFeedback>().HideTextOnCursorLeave = false;
                    this.instantiatedMoreActionButtons.Add(description.Id, buttonInstance);
                }

                this.instantiatedActionBarButtons.Remove(description.Id);
                this.moreActionsButtons.Add(this.instantiatedMoreActionButtons[description.Id]);
            }
        }

        private void RemoveButtonInstanceForContainer(ButtonDescription buttonDescription, bool isActionBarButton)
        {
            if (isActionBarButton)
            {
                this.instantiatedActionBarButtons.Remove(buttonDescription.Id);
            }
            else
            {
                this.instantiatedMoreActionButtons.Remove(buttonDescription.Id);
            }
        }

        private void CreateOrDeleteMoreActionsDescriptionIfRequired()
        {
            bool shouldShowMoreActions = this.CalculateActionBarInitialFreeSlots() < this.extraButtonDescriptions.Count;
            if (shouldShowMoreActions && this.moreActionsDescription == null)
            {
                this.moreActionsDescription = new ButtonDescription
                {
                    IsToggle = true,
                    TextOff = () => this.localization.GetString(() => Resources.Strings.Global_More),
                    TextOn = () => this.localization.GetString(() => Resources.Strings.Global_More),
                    IconOff = CoreResourcesIDs.Materials.Icons.dots,
                    IconOn = CoreResourcesIDs.Materials.Icons.dots,
                    Name = "more",
                };
            }
            else if (!shouldShowMoreActions && this.moreActionsDescription != null)
            {
                this.instantiatedActionBarButtons.Remove(this.moreActionsDescription.Id);
                this.moreActionsDescription = null;
            }
        }

        private int CalculateActionBarInitialFreeSlots()
            => this.AvailableActionSlots - this.GetFollowCloseButtonsCount();

        private int GetFollowCloseButtonsCount()
        {
            int count = 0;

            if (this.includeFollowButton)
            {
                count++;
            }

            if (this.includeCloseButton)
            {
                count++;
            }

            return count;
        }

        private Entity CreateButtonEntity(ButtonDescription description, Guid prefabId)
        {
            var buttonInstance = this.entityLoader.Invoke(description, prefabId);
            buttonInstance.Flags = HideFlags.DontSave;
            buttonInstance.Name = description.Name ?? DefaultButtonEntityName;

            return buttonInstance;
        }
    }
}
