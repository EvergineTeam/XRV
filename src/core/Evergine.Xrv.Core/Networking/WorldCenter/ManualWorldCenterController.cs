// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Effects;
using Evergine.MRTK.Emulation;
using Evergine.MRTK.SDK.Features.UX.Components.Configurators;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// Controller for manual world center user interface. It's in charge
    /// of placing world center marker, lock/unlock it and set its direction.
    /// </summary>
    // This would require some refactoring once we created a reusable orbit menu.
    public class ManualWorldCenterController : Behavior, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        [BindService]
        private XrvService xrv = null;

        [BindEntity(tag: "PART_Manual_Marker_Plate")]
        private Entity plateEntity = null;

        [BindComponent]
        private Transform3D rootTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Plate")]
        private MaterialComponent plateMaterial = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_SetupArrow")]
        private MaterialComponent arrowMaterial = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Current")]
        private MaterialComponent currentArrowMaterial = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Current")]
        private Transform3D currentArrowTransform = null;

        [BindComponent(source: BindComponentSource.Owner)]
        private Transform3D markerTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_SetupArrow")]
        private Transform3D arrowTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Ray")]
        private LineMesh rayMesh = null;

        [BindEntity(tag: "PART_Manual_Marker_LockIndicator")]
        private Entity lockIndicatorEntity = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Menu")]
        private ManualWorldCenterMenu menu = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_Menu")]
        private OrbitMenu orbitMenu = null;

        private HoloGraphic plateHolographic = null;
        private HoloGraphic arrowHolographic = null;
        private HoloGraphic currentArrowHolographic = null;

        private Cursor currentCursor = null;
        private Transform3D currentCursorTransform = null;
        private Transform3D rayTransform = null;
        private Matrix4x4 worldCenterPose;

        private bool isLocked;
        private ThemesSystem themes = null;
        private IEnumerable<CursorTouch> cursors = null;

        private ManipulationMode currentMode;
        private Vector3? lastCursorPosition;
        private bool pinchDetectedForDirectionSetup;

        private ToggleButton menuLockToggle;
        private PressableButton menuMoveButton;
        private PressableButton menuDirectionButton;

        /// <summary>
        /// Raised when <see cref="IsLocked"/> value changes.
        /// </summary>
        public event EventHandler LockedChanged;

        /// <summary>
        /// Raised when <see cref="WorldCenterPose"/> changes.
        /// </summary>
        public event EventHandler WorldCenterPoseChanged;

        private enum ManipulationMode
        {
            Position,
            Direction,
        }

        /// <summary>
        /// Gets a value indicating whether it's waiting for user to initially
        /// place marker using pinch gesture.
        /// </summary>
        [IgnoreEvergine]
        public bool IsWaitingForInitialPinch
        {
            get => !this.plateEntity.IsEnabled;
        }

        /// <summary>
        /// Gets or sets a value indicating whether direction setup is locked.
        /// </summary>
        public bool IsLocked
        {
            get => this.isLocked;
            set
            {
                if (this.isLocked != value)
                {
                    this.isLocked = value;
                    this.OnIsLockedChanged();
                }
            }
        }

        /// <summary>
        /// Gets world center pose defined by arrow direction.
        /// </summary>
        public Matrix4x4 WorldCenterPose
        {
            get => this.worldCenterPose;
            private set
            {
                if (this.worldCenterPose != value)
                {
                    this.worldCenterPose = value;
                    this.WorldCenterPoseChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            if (this.isLocked || this.currentCursor != null)
            {
                // avoid a second cursor interacting
                return;
            }

            this.currentCursor = eventData.Cursor;
            this.RespondToInteractionByCurrentMode(eventData.Position, eventData.Cursor.Pinch);
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (this.currentCursor != null && this.currentCursor != eventData.Cursor)
            {
                return;
            }

            this.RespondToInteractionByCurrentMode(eventData.Position, eventData.Cursor.Pinch);
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            this.EvaluateCurrentCursorResetWhenCursorCollisionEnds();

            if (this.isLocked)
            {
                return;
            }

            this.ResetLastCursorPosition();
            this.UpdateInteractionColors();
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            if (this.isLocked || this.currentCursor != null)
            {
                return;
            }

            this.currentCursor = eventData.Cursor;
            this.RespondToInteractionByCurrentMode(eventData.Position, eventData.Cursor.Pinch);
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            this.RespondToInteractionByCurrentMode(eventData.Position, eventData.Cursor.Pinch);
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            this.EvaluateCurrentCursorResetWhenCursorCollisionEnds();

            if (this.isLocked)
            {
                return;
            }

            this.ResetLastCursorPosition();
            this.UpdateInteractionColors();
        }

        /// <summary>
        /// Invoke this method to confirm that <see cref="WorldCenterPose"/> should
        /// be overriden by new arrow direction.
        /// </summary>
        public void ConfirmCurrentPoseIsNewReferenceValue()
        {
            this.currentArrowTransform.LocalTransform = this.arrowTransform.LocalTransform;
            this.UpdateSetUpArrowVisibility(false);
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                var lineColors = new Color[2];
                this.themes = this.xrv.ThemesSystem;
                if (this.themes != null)
                {
                    this.xrv.ThemesSystem.ThemeUpdated += this.ThemesSystem_ThemeUpdated;
                    lineColors[0] = this.themes.CurrentTheme.SecondaryColor4;
                    lineColors[1] = this.themes.CurrentTheme.PrimaryColor3;
                }
                else
                {
                    // editor only
                    lineColors[0] = Color.White;
                    lineColors[1] = Color.Green;
                }

                this.orbitMenu.CenterTransform = this.rootTransform;

                this.rayMesh.LinePoints.Add(new LinePointInfo() { Position = Vector3.Zero, Thickness = 0.003f, Color = lineColors[0] });
                this.rayMesh.LinePoints.Add(new LinePointInfo() { Position = -Vector3.UnitZ, Thickness = 0.003f, Color = lineColors[1] });
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();

            if (this.themes != null)
            {
                this.themes.ThemeUpdated -= this.ThemesSystem_ThemeUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.plateHolographic = this.plateHolographic ?? new HoloGraphic(this.plateMaterial.Material);
            this.arrowHolographic = this.arrowHolographic ?? new HoloGraphic(this.arrowMaterial.Material);
            this.currentArrowHolographic = this.currentArrowHolographic ?? new HoloGraphic(this.currentArrowMaterial.Material);

            this.SubscribeToMenu();

            if (!Application.Current.IsEditor)
            {
                this.UpdateVisualColors();
                this.arrowTransform.LocalTransform = this.currentArrowTransform.LocalTransform;
                this.UpdateSetUpArrowVisibility(false);
                this.UpdateRayVisibility(false);
                this.plateEntity.IsEnabled = this.menu.Owner.IsEnabled = false;
                this.OnIsLockedChanged();
            }

            this.rayTransform = this.rayMesh.Owner.FindComponent<Transform3D>();
            this.IsLocked = false;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            this.UnsubscribeFromMenu();
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.HandleInitialPinchDetection())
            {
                return;
            }

            if (this.currentMode == ManipulationMode.Direction
                && this.pinchDetectedForDirectionSetup)
            {
                this.HandleOrientationSetup();
            }
        }

        /*
         * Due to our prefab hierarchy, user interaction with menu button
         * is catched by IMixedRealityPointerHandler (as it propagates up
         * on entity hierarchy). We need to ensure some interactions are
         * performed for plate hits only.
         */
        private bool CheckPlateIsTheTargetOfInteraction(Entity target) =>
            ReferenceEquals(target, this.plateEntity);

        private bool HandleInitialPinchDetection()
        {
            if (this.IsWaitingForInitialPinch)
            {
                this.cursors = this.cursors ?? this.Owner.EntityManager.FindComponentsOfType<CursorTouch>(isExactType: false);

                var pinchCursor = this.cursors.FirstOrDefault(cursor => cursor.Pinch);
                if (pinchCursor != null)
                {
                    this.plateEntity.IsEnabled = this.menu.Owner.IsEnabled = true;
                    var cursorTransform = pinchCursor.Owner.FindComponent<Transform3D>();
                    var cameraTransform = this.Managers.RenderManager.ActiveCamera3D.Transform;

                    var cameraProjection = Quaternion.ToEuler(cameraTransform.Orientation);
                    cameraProjection.X = cameraProjection.Z = 0;

                    this.rootTransform.WorldTransform = Matrix4x4.CreateFromTRS(
                        cursorTransform.Position,
                        cameraProjection,
                        Vector3.One);
                    this.UpdateWorldCenterPose();
                }

                return true;
            }

            this.cursors = null;
            return false;
        }

        private void EvaluateStartOfDirectionSetup(bool isPinch)
        {
            if (!this.pinchDetectedForDirectionSetup)
            {
                this.pinchDetectedForDirectionSetup = isPinch;
            }
        }

        private void RespondToInteractionByCurrentMode(Vector3 cursorPosition, bool isPinch)
        {
            if (this.isLocked)
            {
                return;
            }

            switch (this.currentMode)
            {
                case ManipulationMode.Position:
                    this.MoveMarker(cursorPosition, isPinch);
                    break;
                case ManipulationMode.Direction:
                    this.EvaluateStartOfDirectionSetup(isPinch);
                    break;
                default:
                    break;
            }

            this.UpdateInteractionColors();
        }

        private void EvaluateCurrentCursorResetWhenCursorCollisionEnds()
        {
            bool reset =
                this.currentMode == ManipulationMode.Position
                ||
                (this.currentMode == ManipulationMode.Direction
                 &&
                 !this.pinchDetectedForDirectionSetup);

            if (reset)
            {
                this.currentCursor = null;
            }
        }

        private void MoveMarker(Vector3 position, bool isPinch)
        {
            if (!isPinch)
            {
                this.ResetLastCursorPosition();
                return;
            }

            if (!this.lastCursorPosition.HasValue)
            {
                this.lastCursorPosition = position;
                return;
            }

            var delta = position - this.lastCursorPosition.Value;
            this.lastCursorPosition = position;
            this.rootTransform.Position += delta;
            this.UpdateWorldCenterPose();
        }

        private void ResetLastCursorPosition() => this.lastCursorPosition = null;

        private void HandleOrientationSetup()
        {
            if (this.currentCursor == null)
            {
                return;
            }

            /*
             * While user is doing pinch gesture, ray line will be moved
             * and direction arrow will modify its orientation. Once user stops
             * pinching, it ends with orientation interaciton mode. To update
             * orientation again, user should go to the plate and do pinch
             * gesture again.
             */
            if (this.currentCursor.Pinch)
            {
                this.currentCursorTransform ??= this.currentCursor.Owner.FindComponent<Transform3D>();
                this.UpdateRayTransform();
                this.UpdateArrowTransform();
                this.UpdateRayVisibility(true);
                this.UpdateSetUpArrowVisibility(true);
            }
            else
            {
                this.currentCursor = null;
                this.currentCursorTransform = null;
                this.pinchDetectedForDirectionSetup = false;
                this.UpdateRayVisibility(false);
                this.UpdateWorldCenterPose();
            }

            this.UpdateInteractionColors();
        }

        private void UpdateArrowTransform()
        {
            Vector3 localEventPosition = Vector3.Transform(this.currentCursorTransform.Position, this.markerTransform.WorldInverseTransform);
            localEventPosition.Y = 0; // XZ projection
            Vector3 arrowLocalProjection = this.arrowTransform.LocalPosition;
            arrowLocalProjection.Y = 0; // XZ projection

            float angle = Vector3.SignedAngle(arrowLocalProjection, localEventPosition, Vector3.UnitY);
            this.arrowTransform.RotateAround(this.markerTransform.WorldTransform.Translation, Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle));
        }

        private void UpdateRayTransform()
        {
            var position = this.currentCursorTransform.Position;
            var distance = (this.arrowTransform.Position - position).Length();

            this.rayTransform.Scale = new Vector3(1, 1, distance);
            this.rayTransform.LookAt(position, Vector3.Up);

            /*
             * Some "magic numbers" comments here:
             * - line_dots texture counts with 8 pixels, this is that first magic number value.
             * - Second one (2.0) is a simmetry value, which origin could be transparent pixels
             * acting as margin in texture file.
             */
            this.rayMesh.TextureTiling = new Vector2(distance * 8.0f * 2.0f, 1.0f);
        }

        private void UpdateRayVisibility(bool visible) => this.rayMesh.Owner.IsEnabled = visible;

        private void UpdateSetUpArrowVisibility(bool visible) => this.arrowTransform.Owner.IsEnabled = visible;

        private void UpdateWorldCenterPose()
        {
            this.WorldCenterPose = Matrix4x4.CreateFromTRS(
                this.rootTransform.Position,
                this.arrowTransform.Orientation,
                Vector3.One);
        }

        private void OnIsLockedChanged()
        {
            this.lockIndicatorEntity.IsEnabled = this.isLocked;
            this.currentArrowHolographic.Parameters_Alpha = this.isLocked ? 1f : 0.65f;
            Workarounds.ChangeToggleButtonState(this.menuLockToggle.Owner, this.isLocked);

            if (this.IsActivated)
            {
                this.LockedChanged?.Invoke(this, EventArgs.Empty);
            }

            if (!this.isLocked)
            {
                this.SetUpPositionMode();
            }

            this.UpdateMenuButtonStylesByMode();
        }

        private void UpdateInteractionColors()
        {
            var currentTheme = this.themes?.CurrentTheme;
            if (currentTheme != null)
            {
                this.plateMaterial.Material = this.currentCursor != null
                    ? this.themes.GetMaterialByColor(ThemeColor.SecondaryColor1)
                    : this.themes.GetMaterialByColor(ThemeColor.PrimaryColor2);
            }
        }

        private void UpdateVisualColors()
        {
            var currentTheme = this.themes?.CurrentTheme;
            if (currentTheme != null)
            {
                this.arrowHolographic.Albedo = currentTheme.SecondaryColor3;
                this.currentArrowHolographic.Albedo = currentTheme.PrimaryColor1;
                this.UpdateInteractionColors();
            }
        }

        private void SubscribeToMenu()
        {
            if (this.menuLockToggle == null)
            {
                this.menuLockToggle =
                    this.menu.GetButtonEntity(this.menu.GetDescriptorForLockButton())
                    .FindComponent<ToggleButton>();
            }

            if (this.menuMoveButton == null)
            {
                this.menuMoveButton =
                    this.menu.GetButtonEntity(this.menu.GetDescriptorForMoveButton())
                    .FindComponentInChildren<PressableButton>();
            }

            if (this.menuDirectionButton == null)
            {
                this.menuDirectionButton =
                    this.menu.GetButtonEntity(this.menu.GetDescriptorForDirectionButton())
                    .FindComponentInChildren<PressableButton>();
            }

            if (this.menuLockToggle != null)
            {
                this.menuLockToggle.Toggled += this.MenuLockToggle_Toggled;
            }

            if (this.menuMoveButton != null)
            {
                this.menuMoveButton.ButtonReleased += this.MenuMoveButton_ButtonReleased;
            }

            if (this.menuDirectionButton != null)
            {
                this.menuDirectionButton.ButtonReleased += this.MenuDirectionButton_ButtonReleased;
            }
        }

        private void UnsubscribeFromMenu()
        {
            if (this.menuLockToggle != null)
            {
                this.menuLockToggle.Toggled -= this.MenuLockToggle_Toggled;
            }

            if (this.menuMoveButton != null)
            {
                this.menuMoveButton.ButtonReleased -= this.MenuMoveButton_ButtonReleased;
            }

            if (this.menuDirectionButton != null)
            {
                this.menuDirectionButton.ButtonReleased -= this.MenuDirectionButton_ButtonReleased;
            }
        }

        private void SetUpPositionMode()
        {
            if (this.isLocked)
            {
                return;
            }

            this.currentMode = ManipulationMode.Position;
            this.ResetLastCursorPosition();
            this.UpdateMenuButtonStylesByMode();
        }

        private void SetUpDirectionMode()
        {
            if (this.isLocked)
            {
                return;
            }

            this.currentMode = ManipulationMode.Direction;
            this.ResetLastCursorPosition();
            this.UpdateMenuButtonStylesByMode();
        }

        private void UpdateMenuButtonStylesByMode()
        {
            // Temporary approach with some inline colors
            var currentTheme = this.themes?.CurrentTheme;
            if (currentTheme != null)
            {
                var activeColor = Color.Yellow;
                var inactiveColor = currentTheme.PrimaryColor3;

                if (this.menuMoveButton != null)
                {
                    // TODO change this!
                    var configurator = this.menuMoveButton.Owner.Parent?.Parent?.FindComponent<StandardButtonConfigurator>();
                    configurator.PrimaryColor =
                        this.currentMode == ManipulationMode.Position && !this.isLocked ? activeColor : inactiveColor;
                }

                if (this.menuDirectionButton != null)
                {
                    // TODO change this!
                    var configurator = this.menuDirectionButton.Owner.Parent?.Parent?.FindComponent<StandardButtonConfigurator>();
                    configurator.PrimaryColor =
                        this.currentMode == ManipulationMode.Direction && !this.isLocked ? activeColor : inactiveColor;
                }
            }
        }

        private void ThemesSystem_ThemeUpdated(object sender, ThemeUpdatedEventArgs args) =>
            this.UpdateVisualColors();

        private void MenuMoveButton_ButtonReleased(object sender, EventArgs e) =>
            this.SetUpPositionMode();

        private void MenuDirectionButton_ButtonReleased(object sender, EventArgs e) =>
            this.SetUpDirectionMode();

        private void MenuLockToggle_Toggled(object sender, EventArgs e) =>
            this.IsLocked = this.menuLockToggle.IsOn;
    }
}
