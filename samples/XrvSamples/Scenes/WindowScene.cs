﻿using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.UI.Dialogs;
using Evergine.Xrv.Core.UI.Windows;
using System.Threading.Tasks;
using Evergine.Framework.Prefabs;

namespace XrvSamples.Scenes
{
    public class WindowScene : BaseScene
    {
        private const string Text = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum.";

        private PressableButton button1;
        private PressableButton button2;
        private Text3DMesh messageText;
        private WindowsSystem windowsSystem;
        private Window window1;
        private Window window2;
        private string customDistanceKey = nameof(customDistanceKey);

        private PressableButton createAlert;
        private PressableButton createConfirm;
        private Text3DMesh dialogMessageText;

        private AssetsService assetsService;
        private XrvService xrv;

        private bool notificationAutoRunning;
        private Window customTitleWindow;
        private PressableButton customTitleButton;
        private PressableButton customTitleDialogButton;

        protected override void OnPostCreateXRScene()
        {
            this.xrv = Application.Current.Container.Resolve<XrvService>();
            this.xrv.Initialize(this);

            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.windowsSystem = this.xrv.WindowsSystem;
            this.windowsSystem.Distances.SetDistance(this.customDistanceKey, 0.5f);
            this.windowsSystem.OverrideIconMaterial = this.assetsService.Load<Material>(EvergineContent.Materials.EvergineLogo);
            this.messageText = this.Managers.EntityManager.FindAllByTag("message").First().FindComponent<Text3DMesh>();
            this.messageText.Text = string.Empty;

            this.dialogMessageText = this.Managers.EntityManager.FindAllByTag("dialogMessage").First().FindComponent<Text3DMesh>();
            this.dialogMessageText.Text = string.Empty;

            var entityManager = this.Managers.EntityManager;
            
            // Windows sample
            this.button1 = entityManager.FindAllByTag("window1").First().FindComponentInChildren<PressableButton>();
            this.button2 = entityManager.FindAllByTag("window2").First().FindComponentInChildren<PressableButton>();
            this.button1.ButtonReleased += this.ButtonWindow1_ButtonReleased;
            this.button2.ButtonReleased += this.ButtonWindow2_ButtonReleased;

            // Dialogs sample
            this.createAlert = entityManager.FindAllByTag("createAlert").First().FindComponentInChildren<PressableButton>();
            this.createConfirm = entityManager.FindAllByTag("createConfirm").First().FindComponentInChildren<PressableButton>();
            this.createAlert.ButtonReleased += this.CreateAlert_ButtonReleased;
            this.createConfirm.ButtonReleased += this.CreateConfirm_ButtonReleased;

            // Window instances
            this.window1 = this.windowsSystem.CreateWindow(configurator =>
            {
                configurator.Title = "Window #1";
                configurator.Content = this.CreateText3D(
                    Text,
                    new Vector2(0.3f, 0.2f),
                    new Vector3(0.01f, -0.01f, 0f));
                configurator.DisplayLogo = false;
            });

            this.window1.Opened += this.Window1_Opened;
            this.window1.Closed += this.Window1_Closed;

            this.window2 = this.windowsSystem.CreateWindow(configurator =>
            {
                configurator.Title = "Window #2";
                configurator.Size = new Vector2(0.2f, 0.3f);
                configurator.FrontPlateSize = new Vector2(0.2f, 0.25f);
                configurator.FrontPlateOffsets = new Vector2(0f, 0.025f);
                configurator.Content = this.CreateText3D(
                    Text,
                    new Vector2(0.18f, 0.25f),
                    new Vector3(0f, 0.01f, 0f));
            });

            this.window2.DistanceKey = this.customDistanceKey;
            this.window2.Opened += this.Window2_Opened;
            this.window2.Closed += this.Window2_Closed;

            // Notifications
            var notifAuto = entityManager.FindAllByTag("notificationauto").First().FindComponentInChildren<PressableButton>();
            var notifAdd = entityManager.FindAllByTag("notificationadd").First().FindComponentInChildren<PressableButton>();
            notifAuto.ButtonReleased += NotifAuto_ButtonReleased;
            notifAdd.ButtonReleased += NotifAdd_ButtonReleased;

            // Custom title
            this.customTitleWindow = this.windowsSystem.CreateWindow(configurator =>
            {
                var titleContents = this.assetsService.Load<Prefab>(EvergineContent.Prefabs.WindowTitleView_weprefab);
                configurator.TitleView = titleContents.Instantiate();
                configurator.Content = this.CreateText3D(
                    Text,
                    new Vector2(0.3f, 0.2f),
                    new Vector3(0.01f, -0.01f, 0f));
                configurator.DisplayLogo = false;
            });

            this.customTitleButton = entityManager.FindAllByTag("customTitleWindowButton").First().FindComponentInChildren<PressableButton>();
            this.customTitleButton.ButtonPressed += this.CustomTitleButton_ButtonPressed;
            this.customTitleDialogButton = entityManager.FindAllByTag("customTitleDialogButton").First().FindComponentInChildren<PressableButton>();
            this.customTitleDialogButton.ButtonPressed += this.CustomTitleDialogButton_ButtonPressed;
        }

        private void CreateAlert_ButtonReleased(object sender, EventArgs e)
        {
            var alertDialog = this.windowsSystem.ShowAlertDialog("This is an alert dialog!", "This is first line message.\nYou could optionally add a second one", "OK");
            var transform = alertDialog.Owner.FindComponent<Transform3D>();
            alertDialog.Closed += this.Dialog_Closed;
        }

        private void CreateConfirm_ButtonReleased(object sender, EventArgs e)
        {
            var confirmDialog = this.windowsSystem.ShowConfirmationDialog("Confirmation dialog here!", "This is first line message.\nYou could optionally add a second one", "No", "Yes");
            confirmDialog.Closed += this.Dialog_Closed;
        }

        private void Dialog_Closed(object sender, EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Dialog_Closed;
                this.dialogMessageText.Text = $"Dialog result: {dialog.Result ?? "<null>"}";

                if (dialog is ConfirmationDialog confirm && confirm.Result == confirm.AcceptOption.Key)
                {
                    var confirmDialog = this.windowsSystem.ShowConfirmationDialog("test 2!", "this is other message", "nope", "yup");
                    confirmDialog.Closed += this.Dialog_Closed;
                }
            }
        }

        private void ButtonWindow1_ButtonReleased(object sender, EventArgs e)
        {
            if (this.window1.IsOpened)
            {
                this.window1.Close();
            }
            else
            {
                this.window1.Open();
            }
        }

        private void ButtonWindow2_ButtonReleased(object sender, EventArgs e)
        {
            if (this.window2.IsOpened)
            {
                this.window2.Close();
            }
            else
            {
                this.window2.Open();
            }
        }

        private void Window1_Opened(object sender, EventArgs e)
        {
            this.messageText.Text = "Window #1 opened";
        }

        private void Window2_Opened(object sender, EventArgs e)
        {
            this.messageText.Text = "Window #2 opened";
        }

        private void Window1_Closed(object sender, EventArgs e)
        {
            this.messageText.Text = "Window #1 closed";
        }

        private void Window2_Closed(object sender, EventArgs e)
        {
            this.messageText.Text = "Window #2 closed";
        }

        private Entity CreateText3D(string text, Vector2 size, Vector3 offset) =>
            new Entity()
            .AddComponent(new Transform3D()
            {
                LocalPosition = new Vector3(offset.X, offset.Y, offset.Z),
            })
            .AddComponent(new Text3DMesh
            {
                Text = text,
                Size = size,
                ScaleFactor = 0.012f,
                Origin = new Vector2(0.5f, 0.5f),
            })
            .AddComponent(new Text3DRenderer());

        private void NotifAuto_ButtonReleased(object sender, EventArgs e)
        {
            if (this.notificationAutoRunning)
            {
                return;
            }

            this.notificationAutoRunning = true;

            Task.Run(async () =>
            {
                int counter = 0;
                await Task.Delay(3000);

                this.xrv.WindowsSystem.ShowNotification($"Title #{counter}", $"This is the notification #{counter++}");
                await Task.Delay(1000);

                this.xrv.WindowsSystem.ShowNotification($"Title #{counter}", $"This is the notification #{counter++}", EvergineContent.XRV.Materials.LockedIcon);
                this.xrv.WindowsSystem.ShowNotification($"Title #{counter}", $"This is the notification #{counter++}", EvergineContent.XRV.Materials.ColorWheel);
                await Task.Delay(9500);

                this.xrv.WindowsSystem.ShowNotification($"Title #{counter}", $"This is the notification #{counter++}");
                await Task.Delay(2000);
                this.xrv.WindowsSystem.ShowNotification($"Title #{counter}", $"This is the notification #{counter++}", EvergineContent.XRV.Materials.CircleHoverMaterial);

                this.notificationAutoRunning = false;
            });
        }

        private void NotifAdd_ButtonReleased(object sender, EventArgs e)
        {
            this.xrv.WindowsSystem.ShowNotification($"Title at {DateTime.Now.ToLongTimeString()}", $"This is the notification at {DateTime.Now.ToLongTimeString()}");
        }

        private void CustomTitleButton_ButtonPressed(object sender, EventArgs e)
        {
            if (this.customTitleWindow.IsOpened)
            {
                this.customTitleWindow.Close();
            }
            else
            {
                this.customTitleWindow.Open();
            }
        }

        private void CustomTitleDialogButton_ButtonPressed(object sender, EventArgs e)
        {
            var titleContents = this.assetsService.Load<Prefab>(EvergineContent.Prefabs.WindowTitleView_weprefab);
            var alertDialog = this.windowsSystem.ShowAlertDialog(string.Empty, "Message here", "OK");
            alertDialog.Configurator.TitleView = titleContents.Instantiate();
            alertDialog.Closed += this.CustomTitleDialogButton_Closed;
        }

        private void CustomTitleDialogButton_Closed(object sender, EventArgs e)
        {
            if (sender is AlertDialog alertDialog)
            {
                alertDialog.Configurator.TitleView = null;
                alertDialog.Closed -= this.CustomTitleDialogButton_Closed;
            }
        }
    }
}
