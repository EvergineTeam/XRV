using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.Input.Handlers.Manipulation;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System;
using System.Linq;

namespace Xrv.Core.UI.Windows
{
    public class Window : Component
    {
        private Entity closeButton = null;
        private Entity followButton = null;
        private bool allowPin = true;
        private bool enableManipulation = true;

        [BindComponent(source: BindComponentSource.Owner, isExactType: false)]
        private BaseWindowConfigurator configurator = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent(isRequired: false)]
        private SimpleManipulationHandler simpleManipulationHandler;

        [IgnoreEvergine]
        public BaseWindowConfigurator Configurator { get => this.configurator; }

        public bool IsClosed { get => !this.Owner.IsEnabled; }

        public bool DestroyOnClose { get; set; } = false;

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

        public event EventHandler Opened;

        public event EventHandler Closed;

        public void Open()
        {
            if (this.IsClosed)
            {
                this.PlaceInFrontOfUser();
                this.Owner.IsEnabled = true;
                this.Opened?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Close()
        {
            if (!this.IsClosed)
            {
                this.Owner.IsEnabled = false;
                this.Closed?.Invoke(this, EventArgs.Empty);

                if (this.DestroyOnClose)
                {
                    this.Managers.EntityManager.Remove(this.Owner);
                }
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
            var position = camera.Transform.Position + camera.Transform.WorldTransform.Forward * 0.5f;
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
