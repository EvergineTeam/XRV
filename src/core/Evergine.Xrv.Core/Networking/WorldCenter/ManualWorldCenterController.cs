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
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.Utils.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.Networking.WorldCenter
{
    /// <summary>
    /// Controller for manual world center user interface. It's in charge
    /// of placing world center marker, lock/unlock it and set its direction.
    /// </summary>
    public class ManualWorldCenterController : Behavior, IMixedRealityPointerHandler, IMixedRealityTouchHandler
    {
        private readonly TimeConditionChecker nonPinchTimeAfterInitialPinchChecker;

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

        [BindEntity(tag: "PART_Manual_Marker_Menu")]
        private Entity menuEntity = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_Manual_Marker_LockButton")]
        private ToggleButton lockToggleButton = null;

        private HoloGraphic plateHolographic = null;
        private HoloGraphic arrowHolographic = null;
        private HoloGraphic currentArrowHolographic = null;

        private bool isSettingUpOrientation;
        private Cursor currentCursor = null;
        private Transform3D currentCursorTransform = null;
        private Transform3D rayTransform = null;
        private Matrix4x4 worldCenterPose;

        private bool isLocked;
        private bool isCollidingWithPlate;
        private ThemesSystem themes = null;
        private IEnumerable<CursorTouch> cursors = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManualWorldCenterController"/> class.
        /// </summary>
        public ManualWorldCenterController()
        {
            this.nonPinchTimeAfterInitialPinchChecker = new TimeConditionChecker
            {
                DurationTime = TimeSpan.FromSeconds(0.5),
                Policy = new NumberOfTimesPolicy
                {
                    NumberOfTimes = 1,
                },
            };
        }

        /// <summary>
        /// Raised when <see cref="IsLocked"/> value changes.
        /// </summary>
        public event EventHandler LockedChanged;

        /// <summary>
        /// Raised when <see cref="WorldCenterPose"/> changes.
        /// </summary>
        public event EventHandler WorldCenterPoseChanged;

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

            if (this.isLocked)
            {
                return;
            }

            this.isCollidingWithPlate = true;
            this.UpdateVisualColors();
            this.StartOrientationSetup(eventData.Cursor);
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            if (this.isLocked)
            {
                return;
            }

            this.isCollidingWithPlate = false;
            this.UpdateVisualColors();
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            if (this.isLocked)
            {
                return;
            }

            this.isCollidingWithPlate = false;
            this.UpdateVisualColors();
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchStarted(HandTrackingInputEventData eventData)
        {
            if (!this.CheckPlateIsTheTargetOfInteraction(eventData.CurrentTarget))
            {
                return;
            }

            if (this.isLocked)
            {
                return;
            }

            this.isCollidingWithPlate = true;
            this.UpdateVisualColors();
            this.StartOrientationSetup(eventData.Cursor);
        }

        /// <inheritdoc/>
        void IMixedRealityTouchHandler.OnTouchUpdated(HandTrackingInputEventData eventData)
        {
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
                    lineColors[0] = this.themes.CurrentTheme.PrimaryColor2;
                    lineColors[1] = this.themes.CurrentTheme.SecondaryColor3;
                }
                else
                {
                    // editor only
                    lineColors[0] = Color.White;
                    lineColors[1] = Color.Green;
                }

                this.rayMesh.LinePoints.Add(new LinePointInfo() { Position = Vector3.Zero, Thickness = 0.003f, Color = lineColors[0] });
                this.rayMesh.LinePoints.Add(new LinePointInfo() { Position = -Vector3.UnitZ, Thickness = 0.003f, Color = lineColors[1] });

                this.lockToggleButton.Toggled += this.LockToggleButton_Toggled;
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

            this.lockToggleButton.Toggled -= this.LockToggleButton_Toggled;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.isCollidingWithPlate = false;
            this.plateHolographic = this.plateHolographic ?? new HoloGraphic(this.plateMaterial.Material);
            this.arrowHolographic = this.arrowHolographic ?? new HoloGraphic(this.arrowMaterial.Material);
            this.currentArrowHolographic = this.currentArrowHolographic ?? new HoloGraphic(this.currentArrowMaterial.Material);

            if (!Application.Current.IsEditor)
            {
                this.UpdateVisualColors();
                this.arrowTransform.LocalTransform = this.currentArrowTransform.LocalTransform;
                this.UpdateSetUpArrowVisibility(false);
                this.UpdateRayVisibility(false);
                this.plateEntity.IsEnabled = this.menuEntity.IsEnabled = false;
                this.OnIsLockedChanged();
            }

            this.rayTransform = this.rayMesh.Owner.FindComponent<Transform3D>();
            this.IsLocked = false;
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.HandleInitialPinchDetection())
            {
                this.nonPinchTimeAfterInitialPinchChecker.IsEnabled = true;
                return;
            }

            if (this.currentCursor == null || !this.isSettingUpOrientation)
            {
                return;
            }

            /*
             * User should do a pinch gesture to initially place the reference plate.
             * Then, we want that users make pinch gesture again to interact with the plate.
             * In runtime, we need to avoid situations like interaction is immediately activated
             * once user positions the plate. For that, we want that user stops doing
             * pinch gesture for a while, before considering pinch as an interaction start
             * gesture.
             */
            if (!this.nonPinchTimeAfterInitialPinchChecker.Check(gameTime, () => !this.currentCursor.Pinch))
            {
                return;
            }

            this.nonPinchTimeAfterInitialPinchChecker.IsEnabled = false;
            this.HandleOrientationSetup();
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
                    this.plateEntity.IsEnabled = this.menuEntity.IsEnabled = true;
                    var cursorTransform = pinchCursor.Owner.FindComponent<Transform3D>();
                    var cameraTransform = this.Managers.RenderManager.ActiveCamera3D.Transform;

                    var cameraProjection = Quaternion.ToEuler(cameraTransform.Orientation);
                    cameraProjection.X = cameraProjection.Z = 0;

                    this.rootTransform.WorldTransform = Matrix4x4.CreateFromTRS(
                        cursorTransform.Position,
                        cameraProjection,
                        Vector3.One);
                }

                return true;
            }

            this.cursors = null;
            return false;
        }

        private void HandleOrientationSetup()
        {
            /*
             * While user is doing pinch gesture, ray line will be moved
             * and direction arrow will modify its orientation. Once user stops
             * pinching, it ends with orientation interaciton mode. To update
             * orientation again, user should go to the plate and do pinch
             * gesture again.
             */
            if (this.currentCursor.Pinch)
            {
                this.UpdateRayTransform();
                this.UpdateArrowTransform();
                this.UpdateRayVisibility(true);
                this.UpdateSetUpArrowVisibility(true);
            }
            else
            {
                this.currentCursor = null;
                this.isSettingUpOrientation = false;
                this.UpdateRayVisibility(false);
                this.WorldCenterPose = Matrix4x4.CreateFromTRS(
                    this.rootTransform.Position,
                    this.arrowTransform.Orientation,
                    Vector3.One);
            }

            this.UpdateVisualColors();
        }

        private void StartOrientationSetup(Cursor cursor)
        {
            if (this.isLocked)
            {
                return;
            }

            this.currentCursor = cursor;
            this.currentCursorTransform = this.currentCursor.Owner.FindComponent<Transform3D>();
            this.isSettingUpOrientation = true;
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

        private void OnIsLockedChanged()
        {
            this.lockIndicatorEntity.IsEnabled = this.isLocked;
            this.currentArrowHolographic.Parameters_Alpha = this.isLocked ? 1f : 0.65f;

            if (this.IsActivated)
            {
                this.LockedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void UpdateVisualColors()
        {
            var currentTheme = this.themes?.CurrentTheme;
            if (currentTheme != null)
            {
                bool isCheckerRunning =
                    this.nonPinchTimeAfterInitialPinchChecker.IsEnabled
                    &&
                    this.nonPinchTimeAfterInitialPinchChecker.IsInProgress;

                bool interactionEnabled = this.isCollidingWithPlate || this.isSettingUpOrientation;

                this.arrowHolographic.Albedo = currentTheme.SecondaryColor3;
                this.currentArrowHolographic.Albedo = currentTheme.PrimaryColor1;
                this.plateMaterial.Material = interactionEnabled && !isCheckerRunning
                    ? this.themes.GetMaterialByColor(ThemeColor.SecondaryColor1)
                    : this.themes.GetMaterialByColor(ThemeColor.PrimaryColor2);
            }
        }

        private void ThemesSystem_ThemeUpdated(object sender, ThemeUpdatedEventArgs args) =>
            this.UpdateVisualColors();

        private void LockToggleButton_Toggled(object sender, EventArgs args) =>
            this.IsLocked = this.lockToggleButton.IsOn;
    }
}
