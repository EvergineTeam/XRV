// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Audio;
using Evergine.Common.Graphics;
using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Components.Sound;
using Evergine.Components.WorkActions;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.Effects;
using Evergine.Xrv.Core.Extensions;
using Evergine.Xrv.Core.Themes;
using Evergine.Xrv.Core.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Evergine.Xrv.Core.UI.Notifications
{
    /// <summary>
    /// Handles how notifications are about to be displayed while being registered.
    /// </summary>
    public class NotificationsHub : Behavior
    {
        private readonly object lockObject = new object();
        private readonly List<NotificationConfiguration> notifications;
        private readonly List<HoloGraphic> holoGraphicList;

        [BindService]
        private XrvService xrv = null;

        [BindService]
        private AssetsService assetsService = null;

        [BindComponent]
        private Transform3D transform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Title")]
        private Text3DMesh titleMesh = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Text")]
        private Text3DMesh textMesh = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Icon")]
        private MaterialComponent iconMaterial = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Frame")]
        private MaterialComponent borderMaterial = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Plate")]
        private MaterialComponent plateMaterial = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Counter")]
        private MaterialComponent counterMaterial = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_Notification_Counter_Text")]
        private Text3DMesh counterText = null;

        [BindComponent]
        private SoundEmitter3D soundEmitter = null;

        private NotificationConfiguration currentNotification;

        private HoloGraphic iconHoloGraphic;
        private HoloGraphic borderHoloGraphic;
        private HoloGraphic plateHoloGraphic;
        private HoloGraphic counterHoloGraphic;
        private bool isDisplayingHub;
        private bool displayCounter = true;

        private IWorkAction displayHubAnimation;
        private IWorkAction displayItemAnimation;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationsHub"/> class.
        /// </summary>
        public NotificationsHub()
        {
            this.notifications = new List<NotificationConfiguration>();
            this.holoGraphicList = new List<HoloGraphic>();
        }

        /// <summary>
        /// Gets or sets time that takes hub for appearing or disappearing.
        /// </summary>
        public TimeSpan HubAnimationDuration { get; set; } = TimeSpan.FromSeconds(0.3);

        /// <summary>
        /// Gets or sets time that takes hub to show/hide a notification.
        /// </summary>
        public TimeSpan ItemAnimationDuration { get; set; } = TimeSpan.FromSeconds(0.2);

        /// <summary>
        /// Gets or sets amount of time that notification will be presented to the user.
        /// </summary>
        public TimeSpan ItemDisplayDuration { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// Gets or sets sound to be played once notification is being shown.
        /// </summary>
        public AudioBuffer NotificationSound { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether number of notifications counter should be displayed.
        /// </summary>
        public bool DisplayCounter
        {
            get => this.displayCounter;

            set
            {
                if (this.displayCounter != value)
                {
                    this.displayCounter = value;
                    this.UpdateDisplayCounterVisibility();
                }
            }
        }

        /// <summary>
        /// Adds a new notification.
        /// </summary>
        /// <param name="configuration">Notification data.</param>
        public void AddNotification(NotificationConfiguration configuration) =>
            EvergineBackgroundTask.Run(() => this.InternalAddNotification(configuration));

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (!Application.Current.IsEditor)
            {
                this.iconHoloGraphic = new HoloGraphic(this.iconMaterial.Material);
                this.borderHoloGraphic = new HoloGraphic(this.borderMaterial.Material);
                this.plateHoloGraphic = new HoloGraphic(this.plateMaterial.Material);
                this.counterHoloGraphic = new HoloGraphic(this.counterMaterial.Material);

                this.holoGraphicList.Add(this.borderHoloGraphic);
                this.holoGraphicList.Add(this.plateHoloGraphic);
                this.holoGraphicList.Add(this.iconHoloGraphic);
                this.holoGraphicList.Add(this.counterHoloGraphic);

                this.ChangeHubVisibility(false, true, false);
                this.UpdateDisplayCounterVisibility();

                this.OnThemeUpdated(null);
                this.xrv.ThemesSystem.ThemeUpdated += this.ThemesSystem_ThemeUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (!Application.Current.IsEditor)
            {
                this.xrv.ThemesSystem.ThemeUpdated -= this.ThemesSystem_ThemeUpdated;
            }
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            this.UpdatePosition();

            var first = this.notifications.FirstOrDefault();
            if (this.notifications.Any() && this.currentNotification != first)
            {
                this.UpdateDisplayItem(first);

                if (this.isDisplayingHub)
                {
                    this.StartItemAnimation(false);
                }
                else
                {
                    this.ChangeHubVisibility(true, false, false);
                }
            }
            else if (this.isDisplayingHub && first == null)
            {
                this.currentNotification = null;
                this.displayItemAnimation?.TryCancel();
                this.displayItemAnimation = null;
                this.ChangeHubVisibility(false, false, true);
            }
        }

        private void UpdatePosition()
        {
            var camera = this.Managers.RenderManager.ActiveCamera3D;
            var cameraTransform = camera.Transform;
            var distances = this.xrv.WindowsSystem.Distances;
            var distance = distances.GetDistance(Distances.MediumKey) ?? 0.5f;
            var position = cameraTransform.Position + (cameraTransform.WorldTransform.Forward * distance) - (cameraTransform.Up * 0.05f);
            this.transform.Position = Vector3.Lerp(this.transform.Position, position, 0.05f);
        }

        private void UpdateDisplayItem(NotificationConfiguration notification)
        {
            if (this.iconHoloGraphic != null)
            {
                this.holoGraphicList.Remove(this.iconHoloGraphic);
            }

            this.currentNotification = notification;
            this.titleMesh.Text = notification.Title;
            this.textMesh.Text = notification.Text;

            var material = this.assetsService.Load<Material>(notification.IconMaterial);
            this.iconMaterial.Material = material;

            if (material != null)
            {
                var theme = this.xrv.ThemesSystem.CurrentTheme;
                this.iconHoloGraphic = new HoloGraphic(this.iconMaterial.Material);
                this.UpdateHoloGraphicColor(this.iconHoloGraphic, theme.PrimaryColor3);
                this.holoGraphicList.Add(this.iconHoloGraphic);
            }
            else
            {
                this.iconHoloGraphic = null;
            }
        }

        private void InternalAddNotification(NotificationConfiguration configuration)
        {
            lock (this.lockObject)
            {
                this.notifications.Add(configuration);
                this.UpdateDisplayCounterVisibility();
            }
        }

        private void ChangeHubVisibility(bool show, bool immediate, bool skipItem)
        {
            this.isDisplayingHub = show;
            if (show)
            {
                this.plateMaterial.Owner.IsEnabled = true;
            }

            this.displayHubAnimation?.TryCancel();
            this.displayHubAnimation = new FloatAnimationWorkAction(
                    this.Owner,
                    show ? 0f : 1f,
                    show ? 1f : 0f,
                    immediate ? TimeSpan.Zero : this.HubAnimationDuration,
                    EaseFunction.SineOutEase,
                    f =>
                    {
                        var alpha = (byte)(f * 255);
                        Color color = default;

                        if (!skipItem)
                        {
                            color = this.textMesh.Color;
                            color.A = alpha;
                            this.titleMesh.Color = color;
                            this.textMesh.Color = color;
                        }

                        this.counterText.Color = color;

                        foreach (var holoGraphic in this.holoGraphicList)
                        {
                            if (skipItem && holoGraphic == this.iconHoloGraphic)
                            {
                                continue;
                            }

                            if (!this.DisplayCounter && holoGraphic == this.counterHoloGraphic)
                            {
                                continue;
                            }

                            color = holoGraphic.Albedo;
                            color.A = alpha;
                            holoGraphic.Albedo = color;
                        }
                    })
                .ContinueWithAction(() =>
                {
                    this.plateMaterial.Owner.IsEnabled = show;

                    if (show)
                    {
                        this.StartItemAnimation(true);
                    }
                });
            this.displayHubAnimation.Run();
        }

        private void StartItemAnimation(bool skipShow)
        {
            this.PlaySound();
            this.displayItemAnimation?.TryCancel();
            this.displayItemAnimation = new FloatAnimationWorkAction(
                this.Owner,
                0f,
                1f,
                skipShow ? TimeSpan.Zero : this.ItemAnimationDuration,
                EaseFunction.SineOutEase,
                f =>
                {
                    var color = this.textMesh.Color;
                    var alpha = (byte)(f * 255);
                    color.A = alpha;
                    this.titleMesh.Color = color;
                    this.textMesh.Color = color;
                })
                .ContinueWith(new WaitWorkAction(this.ItemDisplayDuration))
                .ContinueWith(new FloatAnimationWorkAction(
                    this.Owner,
                    1f,
                    0f,
                    this.ItemAnimationDuration,
                    EaseFunction.SineOutEase,
                    f =>
                    {
                        var alpha = (byte)(f * 255);
                        var color = this.textMesh.Color;
                        color.A = alpha;
                        this.titleMesh.Color = color;
                        this.textMesh.Color = color;

                        if (this.iconHoloGraphic != null)
                        {
                            color = this.iconHoloGraphic.Albedo;
                            color.A = alpha;
                            this.iconHoloGraphic.Albedo = color;
                        }
                    }))

                // we are experiencing some access violation exceptions in UWP Hololens
                // maybe material resources require some time to complete animation part?
                .ContinueWith(new WaitWorkAction(TimeSpan.FromMilliseconds(50)))
                .ContinueWithAction(() =>
                {
                    lock (this.lockObject)
                    {
                        if (this.notifications.Any())
                        {
                            this.notifications.RemoveAt(0);
                            this.UpdateDisplayCounterVisibility();
                        }
                    }
                });
            this.displayItemAnimation.Run();
        }

        private void PlaySound()
        {
            this.soundEmitter.Stop();
            this.soundEmitter.Audio = this.NotificationSound;
            this.soundEmitter.Play();
        }

        private void UpdateDisplayCounterVisibility()
        {
            if (this.IsAttached && !Application.Current.IsEditor)
            {
                this.counterMaterial.Owner.IsEnabled = this.displayCounter && this.notifications.Count > 1;
                this.counterText.Text = this.notifications.Count.ToString();
            }
        }

        private void ThemesSystem_ThemeUpdated(object sender, ThemeUpdatedEventArgs args) =>
            this.OnThemeUpdated(args.UpdatedColor);

        private void OnThemeUpdated(ThemeColor? updatedColor)
        {
            var theme = this.xrv.ThemesSystem.CurrentTheme;
            if (theme == null)
            {
                return;
            }

            if (updatedColor.HasValue)
            {
                switch (updatedColor.Value)
                {
                    case ThemeColor.PrimaryColor3:
                        this.UpdateHoloGraphicColor(this.borderHoloGraphic, theme.PrimaryColor3);
                        if (this.iconHoloGraphic != null)
                        {
                            this.UpdateHoloGraphicColor(this.iconHoloGraphic, theme.PrimaryColor3);
                        }

                        break;
                    case ThemeColor.SecondaryColor3:
                        this.UpdateHoloGraphicColor(this.counterHoloGraphic, theme.PrimaryColor3);
                        break;
                }
            }
            else
            {
                this.UpdateHoloGraphicColor(this.borderHoloGraphic, theme.PrimaryColor3);
                this.UpdateHoloGraphicColor(this.counterHoloGraphic, theme.SecondaryColor3);
            }
        }

        private void UpdateHoloGraphicColor(HoloGraphic holoGraphic, Color color)
        {
            var currentColor = holoGraphic.Albedo;
            currentColor.R = color.R;
            currentColor.G = color.G;
            currentColor.B = color.B;
            holoGraphic.Albedo = currentColor;
        }
    }
}
