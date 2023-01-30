// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections;
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
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Modules;
using Evergine.Xrv.Core.Themes.Texts;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.Menu
{
    /// <summary>
    /// Manage the Hand menu behavior.
    /// </summary>
    public class HandMenu : Component
    {
        internal const int MinimumNumberOfButtonsPerColumn = 4;
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<MenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;
        private readonly ButtonsIterator buttonsIterator;

        private IWorkAction appearAnimation;
        private IWorkAction extendedAnimation;
        private int numberOfButtonsPerColumn = MinimumNumberOfButtonsPerColumn;
        private bool isDetached = false;

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
            this.buttonsIterator = new ButtonsIterator(this.buttonDescriptions, this.instantiatedButtons);
        }

        /// <summary>
        /// Raised when menu state changes from attached to detached, or vice versa.
        /// </summary>
        public event EventHandler MenuStateChanged;

        /// <summary>
        /// Gets a value indicating whether the Palm was detected for the first time.
        /// </summary>
        public event EventHandler PalmUpDetected;

        /// <summary>
        /// Gets or sets number of buttons per column. When a column can't hold more buttons,
        /// a new column will be added. Minimum number of buttons per column is 4, as this is
        /// the minimal number to have a proper layout when menu is detached.
        /// </summary>
        public int ButtonsPerColumn
        {
            get => this.numberOfButtonsPerColumn;

            set
            {
                if (value < 4)
                {
                    throw new InvalidOperationException($"Minimum number of buttons is {MinimumNumberOfButtonsPerColumn}");
                }

                if (this.numberOfButtonsPerColumn != value && value > 0)
                {
                    this.numberOfButtonsPerColumn = value;
                    this.UpdateLayoutImmediately();
                }
            }
        }

        /// <summary>
        /// Gets button descriptions.
        /// </summary>
        public IList<MenuButtonDescription> ButtonDescriptions { get => this.buttonDescriptions; }

        /// <summary>
        /// Gets a value indicating whether menu is in a detached state.
        /// </summary>
        public bool IsDetached
        {
            get => this.isDetached;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the hand tutorial will be display when application
        /// has started.
        /// </summary>
        public bool DisplayTutorial { get; set; } = true;

        /// <summary>
        /// Retrieves entity that holds a button.
        /// </summary>
        /// <param name="descriptor">Search button descriptor.</param>
        /// <returns>Button entity.</returns>
        /// <exception cref="ArgumentException">Raised when matching descriptor is not found.</exception>
        public Entity GetButtonEntity(MenuButtonDescription descriptor)
        {
            if (!this.instantiatedButtons.ContainsKey(descriptor.Id))
            {
                throw new ArgumentException($"No button match for given descriptor", nameof(descriptor));
            }

            return this.instantiatedButtons[descriptor.Id];
        }

        /// <summary>
        /// Gets button entity that is associated with a given module.
        /// </summary>
        /// <param name="module">Module instance.</param>
        /// <returns>Button entity.</returns>
        public Entity GetModuleButtonEntity(Module module)
        {
            foreach (var buttonEntity in this.buttonsIterator)
            {
                var moduleActivation = buttonEntity.FindComponent<ActivateModuleOnButtonPress>();
                if (moduleActivation?.Module == module)
                {
                    return buttonEntity;
                }
            }

            return null;
        }

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
            this.handMenuTransform.Owner.IsEnabled = false;
            this.UpdateFollowBehavior(false);

            this.UpdateLayoutImmediately();
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
            this.PalmUpDetected?.Invoke(this, null);
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

            this.UpdateLayoutImmediately();
        }

        private void InternalAddButtons(IEnumerable<MenuButtonDescription> buttons)
        {
            var buttonsFactory = new MenuButtonFactory(this.xrvService, this.assetsService);

            foreach (var definition in buttons)
            {
                var buttonInstance = buttonsFactory.CreateInstance(definition);
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

        private void DetachAnimation(bool detach)
        {
            if (this.isDetached == detach)
            {
                return;
            }

            this.isDetached = detach;

            float start = detach ? 0 : 1;
            float end = detach ? 1 : 0;

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
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, this.UpdateLayout))
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

        private void UpdateLayoutImmediately() => this.UpdateLayout(this.isDetached ? 1 : 0);

        private void UpdateLayout(float animationProgress)
        {
            var numberOfButtons = this.buttonDescriptions.Count;
            var numberOfColumns = (int)Math.Ceiling(numberOfButtons / (float)this.numberOfButtonsPerColumn);

            // Front and back plates animation
            this.frontPlateTransform.Scale = Vector3.Lerp(new Vector3(numberOfColumns, this.numberOfButtonsPerColumn, 1), new Vector3(this.numberOfButtonsPerColumn, numberOfColumns, 1), animationProgress);
            this.backPlateTransform.Scale = Vector3.Lerp(new Vector3(numberOfColumns, 1, 1), new Vector3(this.numberOfButtonsPerColumn, 1, 1), animationProgress);

            // Header animation
            this.detachButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(ButtonWidthOverTwo, 0, 0.003f), new Vector3(ButtonWidthOverTwo + (ButtonWidth * (this.numberOfButtonsPerColumn - 1)), 0, 0.003f), animationProgress);
            this.followButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidthOverTwo, 0, 0.003f), new Vector3(-ButtonWidthOverTwo + (ButtonWidth * (this.numberOfButtonsPerColumn - 1)), 0, 0.003f), animationProgress);

            this.textTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidth * 2, 0, 0.003f), new Vector3(0.015f, 0, 0.003f), animationProgress);

            var textColor = this.text3DMesh.Owner.FindComponent<Text3dStyle>()?.GetTextColor() ?? Color.White;
            this.text3DMesh.Color = Color.Lerp(Color.Transparent, textColor, animationProgress);

            // Buttons animation
            int i = 0;

            foreach (var button in this.buttonsIterator)
            {
                var buttonTransform = button.FindComponent<Transform3D>();
                buttonTransform.LocalPosition = Vector3.Lerp(
                    new Vector3(ButtonWidthOverTwo + (ButtonWidth * (i / this.numberOfButtonsPerColumn)), -ButtonWidthOverTwo - ((i % this.numberOfButtonsPerColumn) * ButtonWidth), 0),
                    new Vector3(ButtonWidthOverTwo + ((i % this.numberOfButtonsPerColumn) * ButtonWidth), -ButtonWidthOverTwo - (ButtonWidth * (i / this.numberOfButtonsPerColumn)), 0),
                    animationProgress);

                i++;
            }
        }

        private void DetachButtonToggle_Toggled(object sender, EventArgs e)
        {
            // Reset hand menu local position, as user may have moved it when detached
            if (!this.detachButtonToggle.IsOn)
            {
                this.handMenuTransform.LocalPosition = Vector3.Zero;
            }

            this.UpdateFollowBehavior(false);
            this.DetachAnimation(this.detachButtonToggle.IsOn);
            this.MenuStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void FollowButtonToggle_Toggled(object sender, EventArgs e) =>
            this.UpdateFollowBehavior(this.followButtonToggle.IsOn);

        private void UpdateFollowBehavior(bool followEnabled)
        {
            this.tagAlong.IsEnabled = followEnabled;
            this.manipulationHandler.IsEnabled = !followEnabled;
            Workarounds.ChangeToggleButtonState(this.followButtonToggle.Owner, followEnabled);
        }

        internal class ButtonsIterator : IEnumerable<Entity>
        {
            private readonly IEnumerable<MenuButtonDescription> descriptors;
            private readonly Dictionary<Guid, Entity> instances;

            public ButtonsIterator(IEnumerable<MenuButtonDescription> descriptors, Dictionary<Guid, Entity> instances)
            {
                this.descriptors = descriptors;
                this.instances = instances;
            }

            public IEnumerator<Entity> GetEnumerator()
            {
                foreach (var descriptor in this.descriptors.OrderBy(d => d.Order))
                {
                    yield return this.instances[descriptor.Id];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }

        internal static class VoiceCommands
        {
            public static string DetachMenu = "Detach menu";
        }
    }
}
