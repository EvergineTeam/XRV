﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// Manual world center menu generation.
    /// </summary>
    // This would require some refactoring once we created a reusable orbit menu.
    public class ManualWorldCenterMenu : Component
    {
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<ButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private bool menuExtended = false;
        private IWorkAction extendedAnimation;
        private int numberOfButtons;

        private ButtonDescription lockButtonDesc;
        private ButtonDescription moveButtonDesc;
        private ButtonDescription directionButtonDesc;
        private bool animating;

        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_backplate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_frontplate")]
        private Transform3D frontPlateTransform = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_frontplate", isRecursive: true)]
        private Entity frontPlate = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_buttonsContainer", isRecursive: true)]
        private Entity buttonsContainer = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_optionsButton")]
        private ToggleButton optionsButtonToggle = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Manual_Marker_Menu_optionsButton")]
        private ToggleStateManager options = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualWorldCenterMenu"/> class.
        /// </summary>
        public ManualWorldCenterMenu()
        {
            this.buttonDescriptions = new ObservableCollection<ButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        /// <summary>
        /// Gets the Button descriptions list.
        /// </summary>
        public IList<ButtonDescription> ButtonDescriptions { get => this.buttonDescriptions; }

        // TODO: Create a generic horizontal menu component that we can reuse (mix with
        // OrbitMenu). It would be great that MenuButtonDescription supports ICommand,
        // so user can have an easy way to have a callback. Will add this as new
        // PBI to backlog.
        internal Entity GetButtonEntity(ButtonDescription descriptor)
        {
            if (!this.instantiatedButtons.ContainsKey(descriptor.Id))
            {
                throw new ArgumentException($"No button match for given descriptor", nameof(descriptor));
            }

            return this.instantiatedButtons[descriptor.Id];
        }

        internal ButtonDescription GetDescriptorForLockButton() => this.lockButtonDesc;

        internal ButtonDescription GetDescriptorForMoveButton() => this.moveButtonDesc;

        internal ButtonDescription GetDescriptorForDirectionButton() => this.directionButtonDesc;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                // Lock button
                this.lockButtonDesc = new ButtonDescription
                {
                    IsToggle = true,
                    IconOn = CoreResourcesIDs.Materials.Icons.Unlock,
                    IconOff = CoreResourcesIDs.Materials.Icons.Lock,
                    TextOn = () => this.xrvService.Localization.GetString(() => Resources.Strings.WorldCenter_Manual_Unlock),
                    TextOff = () => this.xrvService.Localization.GetString(() => Resources.Strings.WorldCenter_Manual_Lock),
                };
                this.buttonDescriptions.Add(this.lockButtonDesc);

                // Move button
                this.moveButtonDesc = new ButtonDescription
                {
                    IsToggle = false,
                    IconOn = CoreResourcesIDs.Materials.Icons.Move,
                    TextOn = () => this.xrvService.Localization.GetString(() => Resources.Strings.WorldCenter_Manual_Move),
                };
                this.buttonDescriptions.Add(this.moveButtonDesc);

                // Direction button
                this.directionButtonDesc = new ButtonDescription
                {
                    IsToggle = false,
                    IconOn = CoreResourcesIDs.Materials.Icons.Direction,
                    TextOn = () => this.xrvService.Localization.GetString(() => Resources.Strings.WorldCenter_Manual_Direction),
                };
                this.buttonDescriptions.Add(this.directionButtonDesc);
                this.InternalAddButtons(this.buttonDescriptions);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.optionsButtonToggle.Toggled += this.OptionsButtonToggle_Toggled;

            this.ReorderButtons();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            this.optionsButtonToggle.Toggled -= this.OptionsButtonToggle_Toggled;
            this.ExtendedAnimation(false, false);
            Workarounds.ChangeToggleButtonState(this.optionsButtonToggle.Owner, false);
        }

        private void OptionsButtonToggle_Toggled(object sender, EventArgs e)
        {
            this.ExtendedAnimation(this.optionsButtonToggle.IsOn, true);
        }

        private void InternalAddButtons(IEnumerable<ButtonDescription> buttons)
        {
            var buttonsFactory = new ButtonFactory(this.assetsService);

            foreach (var definition in buttons)
            {
                var buttonInstance = buttonsFactory.CreateInstance(definition);
                this.buttonsContainer.AddChild(buttonInstance);
                this.instantiatedButtons.Add(definition.Id, buttonInstance);
            }
        }

        /// <summary>
        /// Reorder buttons position. Allows add or delete dynamically buttons.
        /// </summary>
        private void ReorderButtons()
        {
            this.numberOfButtons = this.buttonDescriptions.Count;
            this.frontPlate.IsEnabled = false;
            var initialX = -((ButtonWidth * this.numberOfButtons) / 2) + ButtonWidthOverTwo;

            // Add buttons
            int i = 0;
            foreach (var button in this.instantiatedButtons.Values)
            {
                var buttonTransform = button.FindComponent<Transform3D>();
                buttonTransform.LocalPosition = new Vector3(initialX + (i * ButtonWidth), 0, 0);
                i++;
                button.IsEnabled = false;
            }
        }

        private void ExtendedAnimation(bool extended, bool animate)
        {
            if (this.menuExtended == extended)
            {
                return;
            }

            this.menuExtended = extended;
            this.animating = true;

            float start = extended ? 0 : 1;
            float end = extended ? 1 : 0;

            this.extendedAnimation?.TryCancel();
            this.extendedAnimation = new ActionWorkAction(() =>
            {
                if (end == 1)
                {
                    this.frontPlate.IsEnabled = true;
                }

                if (end == 0)
                {
                    foreach (var button in this.instantiatedButtons.Values)
                    {
                        button.IsEnabled = false;
                    }
                }
            })
            .ContinueWith(
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(animate ? 0.4f : 0f), EaseFunction.SineInOutEase, (f) =>
                {
                    // Front and back plates animation
                    this.frontPlateTransform.Scale = Vector3.Lerp(new Vector3(1, 1, 1), new Vector3(this.numberOfButtons, 1, 1), f);
                    this.backPlateTransform.LocalPosition = Vector3.Lerp(Vector3.Zero, new Vector3(-((ButtonWidth * this.numberOfButtons) / 2) - ButtonWidthOverTwo, 0, 0), f);
                }))
            .ContinueWith(
                new ActionWorkAction(() =>
                {
                    if (end == 0)
                    {
                        this.frontPlate.IsEnabled = false;
                    }

                    if (end == 1)
                    {
                        foreach (var button in this.instantiatedButtons.Values)
                        {
                            button.IsEnabled = true;
                        }
                    }

                    this.animating = false;
                }));
            this.extendedAnimation.Run();
        }

        private void LockButton_Toggled(object sender, EventArgs e)
        {
            if (this.animating)
            {
                return;
            }

            this.options.ChangeState(this.options.States.ElementAt(0));
            this.ExtendedAnimation(false, true);
        }
    }
}
