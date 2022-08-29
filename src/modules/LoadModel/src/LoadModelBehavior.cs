// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xrv.Core;
using Xrv.Core.Menu;

namespace Xrv.LoadModel
{
    /// <summary>
    /// Manages the Load Model menu.
    /// </summary>
    public class LoadModelBehavior : Component
    {
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<MenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private Entity modelEntity;
        private Matrix4x4 modelEntityWorld;
        private bool menuExtended = false;
        private IWorkAction extendedAnimation;
        private int numberOfButtons;

        private MenuButtonDescription lockButtonDesc;
        private Entity lockButton;
        private MenuButtonDescription resetButtonDesc;
        private MenuButtonDescription deleteButtonDesc;

        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_loading")]
        private Entity Loading = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_loadingText")]
        private Text3DMesh LoadingText = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_lockedIcon")]
        private Entity LockedIcon = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_backplate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_frontplate")]
        private Transform3D frontPlateTransform = null;

        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_menu")]
        private Entity menu = null;

        private Entity frontPlate = null;
        private Entity buttonsContainer = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_optionsButton")]
        private ToggleButton optionsButtonToggle = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadModelBehavior"/> class.
        /// </summary>
        public LoadModelBehavior()
        {
            this.buttonDescriptions = new ObservableCollection<MenuButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        /// <summary>
        /// Gets or sets the model loaded entity.
        /// </summary>
        public Entity ModelEntity
        {
            get => this.modelEntity;
            set
            {
                if (value != null)
                {
                    this.modelEntity = value;
                    this.modelEntityWorld = this.modelEntity.FindComponent<Transform3D>().WorldTransform;
                    this.AddManipulatorComponents(this.modelEntity);
                    this.Owner.AddChild(this.modelEntity);
                    this.Loading.IsEnabled = false;
                    this.LockedIcon.IsEnabled = false;
                }
            }
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
                this.frontPlate = this.menu.FindChildrenByTag("PART_manipulator_frontplate").First();
                this.buttonsContainer = this.menu.FindChildrenByTag("PART_manipulator_buttonsContainer").First();

                // Lock button
                this.lockButtonDesc = new MenuButtonDescription
                {
                    IsToggle = true,
                    IconOn = LoadModelResourceIDs.Materials.Icons.locked,
                    IconOff = LoadModelResourceIDs.Materials.Icons.locked,
                    TextOn = "Unlock",
                    TextOff = "Lock",
                };
                this.buttonDescriptions.Add(this.lockButtonDesc);

                // Reset button
                this.resetButtonDesc = new MenuButtonDescription
                {
                    IsToggle = false,
                    IconOn = LoadModelResourceIDs.Materials.Icons.reset,
                    TextOn = "Reset",
                };
                this.buttonDescriptions.Add(this.resetButtonDesc);

                // Delete button
                this.deleteButtonDesc = new MenuButtonDescription
                {
                    IsToggle = false,
                    IconOn = LoadModelResourceIDs.Materials.Icons.delete,
                    TextOn = "Delete",
                };
                this.buttonDescriptions.Add(this.deleteButtonDesc);

                this.InternalAddButtons(this.buttonDescriptions);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.LockedIcon.IsEnabled = false;
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

            this.lockButton = this.instantiatedButtons[this.lockButtonDesc.Id];
            this.lockButton.FindComponent<ToggleButton>().Toggled += this.LockButton_Toggled;

            var resetButton = this.instantiatedButtons[this.resetButtonDesc.Id];
            resetButton.FindComponentInChildren<PressableButton>().ButtonReleased += this.ResetButton_Released;

            var deleteButton = this.instantiatedButtons[this.deleteButtonDesc.Id];
            deleteButton.FindComponentInChildren<PressableButton>().ButtonReleased += this.DeleteButton_Released;
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

        private void LockButton_Toggled(object sender, EventArgs e)
        {
            if (sender is ToggleButton lockButton)
            {
                this.LockedIcon.IsEnabled = lockButton.IsOn;
            }
        }

        private void ResetButton_Released(object sender, EventArgs e)
        {
            this.modelEntity.FindComponent<Transform3D>().WorldTransform = this.modelEntityWorld;
        }

        private void DeleteButton_Released(object sender, EventArgs e)
        {
        }

        private void AddManipulatorComponents(Entity entity)
        {
            entity.AddComponent(new BoxCollider3D());
            entity.AddComponent(new StaticBody3D());
            entity.AddComponent(new Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.BoundingBox()
            {
                AutoCalculate = false,
                BoxMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxVisual),
                BoxGrabbedMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxVisualGrabbed),
                ShowWireframe = true,
                ShowXRotationHandle = true,
                ShowYRotationHandle = true,
                ShowZRotationHandle = true,
                WireframeMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxWireframe),
                HandleMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxHandleBlue),
                HandleGrabbedMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxHandleBlueGrabbed),
                RotationHandlePrefab = this.assetsService.Load<Prefab>(LoadModelResourceIDs.MRTK.Prefabs.BoundingBox_RotateHandle_weprefab),
            });
        }
    }
}
