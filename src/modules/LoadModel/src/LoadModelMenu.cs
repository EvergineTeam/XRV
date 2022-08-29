// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xrv.Core;
using Xrv.Core.Menu;

namespace Xrv.LoadModel
{
    /// <summary>
    /// Manages the Load Model menu.
    /// </summary>
    public class LoadModelMenu : Component
    {
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<MenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private bool menuExtended = false;
        private IWorkAction extendedAnimation;
        private int numberOfButtons;

        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_backplate")]
        private Transform3D backPlateTransform = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_frontplate")]
        private Entity frontPlate = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_frontplate")]
        private Transform3D frontPlateTransform = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_buttonsContainer")]
        private Entity buttonsContainer = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_optionsButton")]
        private ToggleButton optionsButtonToggle = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadModelMenu"/> class.
        /// </summary>
        public LoadModelMenu()
        {
            this.buttonDescriptions = new ObservableCollection<MenuButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        /// <summary>
        /// Gets the Button descriptions list.
        /// </summary>
        public IList<MenuButtonDescription> ButtonDescriptions { get => this.buttonDescriptions; }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                // Lock button
                this.buttonDescriptions.Add(new MenuButtonDescription
                {
                    IsToggle = true,
                    IconOn = LoadModelResourceIDs.Materials.Icons.locked,
                    IconOff = LoadModelResourceIDs.Materials.Icons.locked,
                    TextOn = "Unlock",
                    TextOff = "Lock",
                });

                // Reset button
                this.buttonDescriptions.Add(new MenuButtonDescription
                {
                    IsToggle = false,
                    IconOn = LoadModelResourceIDs.Materials.Icons.reset,
                    TextOn = "Reset",
                });

                // Delete button
                this.buttonDescriptions.Add(new MenuButtonDescription
                {
                    IsToggle = false,
                    IconOn = LoadModelResourceIDs.Materials.Icons.delete,
                    TextOn = "Delete",
                });

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
        }

        private void OptionsButtonToggle_Toggled(object sender, EventArgs e)
        {
            this.ExtendedAnimation(this.optionsButtonToggle.IsOn);
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

        private void ExtendedAnimation(bool extended)
        {
            if (this.menuExtended == extended)
            {
                return;
            }

            this.menuExtended = extended;

            float start = extended ? 0 : 1;
            float end = extended ? 1 : 0;

            this.extendedAnimation?.Cancel();
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
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, (f) =>
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
                }));
            this.extendedAnimation.Run();
        }
    }
}
