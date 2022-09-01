// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Linq;

namespace Xrv.Core.UI.Windows
{
    /// <summary>
    /// Component to work with windows.
    /// </summary>
    public class Window : Component
    {
        /// <summary>
        /// XRV service instance.
        /// </summary>
        [BindService]
        protected XrvService xrvService = null;

        private Entity closeButton = null;
        private Entity followButton = null;
        private bool allowPin = true;
        private bool enableManipulation = true;

        [BindComponent(source: BindComponentSource.Owner, isExactType: false)]
        private BaseWindowConfigurator configurator = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent(isRequired: false)]
        private SimpleManipulationHandler simpleManipulationHandler = null;

        [BindComponent]
        private WindowTagAlong windowTagAlong = null;

        /// <summary>
        /// Raised when window is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Raised when window is closed.
        /// </summary>
        public event EventHandler Closed;

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
        /// Gets or sets distance key to be used from <see cref="Distances"/> to get distance
        /// where window should be displayed to the user when is opened.
        /// </summary>
        [IgnoreEvergine]
        public string DistanceKey { get; set; }

        /// <summary>
        /// Opens the window, if it's not already opened.
        /// </summary>
        public void Open()
        {
            if (!this.IsOpened)
            {
                this.PlaceInFrontOfUser();
                this.Owner.IsEnabled = true;
                this.Opened?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Closes the window, if it's not already closed.
        /// </summary>
        public void Close()
        {
            if (this.IsOpened)
            {
                this.UpdateFollowBehavior(false);
                this.Owner.IsEnabled = false;
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.closeButton = this.Owner.FindChildrenByTag("PART_window_close", true).First();
                this.followButton = this.Owner.FindChildrenByTag("PART_window_follow", true).First();
                this.SubscribeEvents();
                this.UpdateFollowBehavior(false);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateAllowPin();
            this.UpdateEnableManipulation();
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.UnsubscribeEvents();
        }

        /// <summary>
        /// Gets distance where window will be displayed when opened.
        /// </summary>
        /// <returns>Distance.</returns>
        protected virtual float GetOpenDistance()
        {
            var distances = this.xrvService.WindowSystem.Distances;
            return distances.GetDistanceOrAlternative(this.DistanceKey, Distances.MediumKey);
        }

        private void SubscribeEvents()
        {
            var followButtonPressable = this.followButton.FindComponentInChildren<ToggleButton>();
            followButtonPressable.Toggled += this.FollowButtonToggled;

            var closeButtonPressable = this.closeButton.FindComponentInChildren<PressableButton>();
            closeButtonPressable.ButtonReleased += this.CloseButtonReleased;
        }

        private void UnsubscribeEvents()
        {
            var followButtonPressable = this.followButton.FindComponentInChildren<ToggleButton>();
            followButtonPressable.Toggled -= this.FollowButtonToggled;

            var closeButtonPressable = this.closeButton.FindComponentInChildren<PressableButton>();
            closeButtonPressable.ButtonReleased -= this.CloseButtonReleased;
        }

        private void UpdateAllowPin()
        {
            if (this.IsAttached)
            {
                this.followButton.IsEnabled = this.allowPin;
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

            // default LookAt makes window to be oriented backwards to the camera
            this.transform.LookAt(camera.Transform.Position);
            this.transform.RotateAround(position, Vector3.Up, MathHelper.Pi);
        }

        private void UpdateFollowBehavior(bool followEnabled)
        {
            this.EnableManipulation = !followEnabled;
            this.windowTagAlong.IsEnabled = followEnabled;
            Workarounds.ChangeToggleButtonState(this.followButton, followEnabled);
        }

        private void UpdateEnableManipulation()
        {
            if (this.simpleManipulationHandler != null)
            {
                this.simpleManipulationHandler.IsEnabled = this.enableManipulation;
            }
        }
    }
}
