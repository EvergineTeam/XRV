using Evergine.Common.Attributes;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;

namespace Xrv.Core.UI.Windows
{
    public class Window : Component
    {
        private Entity closeButton = null;
        private bool allowPin = true;

        [BindComponent(source: BindComponentSource.Owner, isExactType: false)]
        private BaseWindowConfigurator configurator = null;

        [BindComponent]
        private Transform3D transform;

        protected Entity pinButtonEntity;

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
                this.pinButtonEntity = this.Owner.FindChildrenByTag("PART_window_follow", true).First();
                this.SubscribeEvents();
            }

            return attached;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            this.UpdateAllowPin();
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (this.closeButton != null)
            {
                var pressable = this.closeButton.FindComponentInChildren<PressableButton>();
                pressable.ButtonReleased += this.CloseButtonReleased;
            }
        }

        private void UnsubscribeEvents()
        {
            if (this.closeButton != null)
            {
                var pressable = this.closeButton.FindComponentInChildren<PressableButton>();
                pressable.ButtonReleased -= this.CloseButtonReleased;
            }
        }

        private void UpdateAllowPin()
        {
            if (this.IsAttached)
            {
                this.pinButtonEntity.IsEnabled = this.allowPin;
            }
        }

        private void CloseButtonReleased(object sender, EventArgs e) => this.Close();

        private void PlaceInFrontOfUser()
        {
            var camera = this.Managers.RenderManager.ActiveCamera3D;
            var position = camera.Transform.Position + camera.Transform.WorldTransform.Forward;
            this.transform.Position = position;

            // default LookAt makes window to be oriented backwards to the camera
            this.transform.LookAt(camera.Transform.Position);
            this.transform.RotateAround(position, this.transform.WorldTransform.Up, MathHelper.Pi);
        }
    }
}
