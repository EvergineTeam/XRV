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
        private Entity closeButton = null;
        private Entity followButton = null;
        private bool allowPin = true;
        private bool enableManipulation = true;

        [BindService]
        protected XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Owner, isExactType: false)]
        private BaseWindowConfigurator configurator = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent(isRequired: false)]
        private SimpleManipulationHandler simpleManipulationHandler = null;

        [IgnoreEvergine]
        public BaseWindowConfigurator Configurator { get => this.configurator; }

        public bool IsOpened { get => this.Owner.IsEnabled; }

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

        [IgnoreEvergine]
        public string DistanceKey { get; set; }

        public event EventHandler Opened;

        public event EventHandler Closed;

        public void Open()
        {
            if (!this.IsOpened)
            {
                this.PlaceInFrontOfUser();
                this.Owner.IsEnabled = true;
                this.Opened?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Close()
        {
            if (this.IsOpened)
            {
                this.Owner.IsEnabled = false;
                this.Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.closeButton = this.Owner.FindChildrenByTag("PART_window_close", true).First();
                this.followButton = this.Owner.FindChildrenByTag("PART_window_follow", true).First();
                this.SubscribeEvents();
            }

            return attached;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateAllowPin();
            this.UpdateEnableManipulation();
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.UnsubscribeEvents();
        }

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
