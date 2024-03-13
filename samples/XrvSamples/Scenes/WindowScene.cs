using Evergine.Components.Fonts;
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
using System.Collections.Generic;
using Evergine.Xrv.Core.UI.Buttons;
using Evergine.Framework.Threading;
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;

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

        private Window actionButtonsWindow;
        private PressableButton actionButtonsShowWindowButton;
        private List<ButtonDescription> actionButtonsDescriptors;
        private PressableButton actionButtonsLessActionsButton;
        private PressableButton actionButtonsMoreActionsButton;
        private PressableButton actionButtonsLessSlotsButton;
        private PressableButton actionButtonsMoreSlotsButton;
        private Text3DMesh actionButtonActionsCountText;
        private Text3DMesh actionButtonSlotsCountText;
        private Text3DMesh actionButtonsCallbackText;
        private ToggleButton actionButtonsCloseToggle;
        private PressableButton actionButtonsToggleChange;
        private ToggleButton actionButtonsTogglePlacement;
        private ToggleButton actionButtonsToggleBehavior;

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

            // Action buttons
            this.actionButtonsWindow = this.windowsSystem.CreateWindow(configurator =>
            {
                configurator.Content = this.CreateText3D(
                    "Here the window contents",
                    new Vector2(0.3f, 0.2f),
                    new Vector3(0.01f, -0.01f, 0f));
            });
            this.actionButtonsShowWindowButton = entityManager.FindAllByTag("actionButtonsShowButton").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsShowWindowButton.ButtonPressed += this.ActionButtonsShowWindowButton_ButtonPressed;
            this.actionButtonsDescriptors = new List<ButtonDescription>();
            this.actionButtonsLessActionsButton = entityManager.FindAllByTag("actionButtonsLessActionsButton").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsLessActionsButton.ButtonPressed += this.ActionButtonsLessActionsButton_ButtonPressed;
            this.actionButtonsMoreActionsButton = entityManager.FindAllByTag("actionButtonsMoreActionsButton").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsMoreActionsButton.ButtonPressed += this.ActionButtonsMoreActionsButton_ButtonPressed;
            this.actionButtonsLessSlotsButton = entityManager.FindAllByTag("actionButtonsLessSlotsButton").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsLessSlotsButton.ButtonPressed += this.ActionButtonsLessSlotsButton_ButtonPressed;
            this.actionButtonsMoreSlotsButton = entityManager.FindAllByTag("actionButtonsMoreSlotsButton").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsMoreSlotsButton.ButtonPressed += this.ActionButtonsMoreSlotsButton_ButtonPressed;

            this.actionButtonActionsCountText = entityManager.FindAllByTag("actionButtonActionsCountText").First().FindComponentInChildren<Text3DMesh>();
            this.actionButtonSlotsCountText = entityManager.FindAllByTag("actionButtonsSlotsCountText").First().FindComponentInChildren<Text3DMesh>();
            this.actionButtonsCallbackText = entityManager.FindAllByTag("actionButtonsCallbackText").First().FindComponentInChildren<Text3DMesh>();
            this.actionButtonsCloseToggle = entityManager.FindAllByTag("actionButtonsToggleClose").First().FindComponentInChildren<ToggleButton>();
            this.actionButtonsCloseToggle.Toggled += this.ActionButtonsCloseToggle_Toggled;

            this.actionButtonsToggleChange = entityManager.FindAllByTag("actionButtonsToggleChange").First().FindComponentInChildren<PressableButton>();
            this.actionButtonsToggleChange.ButtonPressed += this.ActionButtonsToggleChange_ButtonPressed;
            
            this.actionButtonsTogglePlacement = entityManager.FindAllByTag("actionButtonsTogglePlacement").First().FindComponentInChildren<ToggleButton>();
            this.actionButtonsTogglePlacement.Toggled += this.ActionButtonsTogglePlacement_Toggled;

            this.actionButtonsToggleBehavior = entityManager.FindAllByTag("actionButtonsToggleMoreBehavior").First().FindComponentInChildren<ToggleButton>();
            this.actionButtonsToggleBehavior.Toggled += this.ActionButtonsToggleBehavior_Toggled;

            this.actionButtonsWindow.ActionButtonPressed += this.ActionButtonsWindow_ActionButtonPressed;

            EvergineForegroundTask.Run(() =>
            {
                this.AddSampleActionButton();
                this.AddSampleActionButton();
                this.UpdateActionButtonCounters();
            });
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

        private void AddSampleActionButton(bool isToggleSample = false)
        {
            var number = this.actionButtonsDescriptors.Count + 1;
            var descriptor = new ButtonDescription
            {
                TextOn = () => $"Option {number}",
                IconOn = EvergineContent.Materials.Icons.Pi,
                Name = $"Option {number}",
            };

            if (isToggleSample)
            {
                descriptor.IsToggle = true;
                descriptor.TextOff = () => $"#Off {number}";
                descriptor.IconOff = EvergineContent.Materials.Icons.cross;
            }

            this.actionButtonsDescriptors.Add(descriptor);
            this.actionButtonsWindow.ExtraActionButtons.Add(descriptor);
            this.UpdateActionButtonCounters();

            if (isToggleSample)
            {
                Entity buttonEntity = this.actionButtonsWindow.GetActionButtonEntity(descriptor);
                buttonEntity.AddComponent(new AutomaticToggleChange());
            }
        }

        private void ActionButtonsWindow_ActionButtonPressed(object sender, ActionButtonPressedEventArgs args)
        {
            this.actionButtonsCallbackText.Text = $"{(args.IsOn ? args.Description.TextOn() : args.Description.TextOff())} callback executed";
        }

        private void UpdateActionButtonCounters()
        {
            this.actionButtonActionsCountText.Text = this.actionButtonsWindow.ExtraActionButtons.Count.ToString();
            this.actionButtonSlotsCountText.Text = this.actionButtonsWindow.AvailableActionSlots.ToString();
        }

        private void ActionButtonsShowWindowButton_ButtonPressed(object sender, EventArgs e) => this.actionButtonsWindow.Open();

        private void ActionButtonsLessActionsButton_ButtonPressed(object sender, EventArgs e)
        {
            if (this.actionButtonsDescriptors.Any())
            {
                var descriptor = this.actionButtonsDescriptors.Last();
                this.actionButtonsDescriptors.Remove(descriptor);
                this.actionButtonsWindow.ExtraActionButtons.Remove(descriptor);
            }

            this.UpdateActionButtonCounters();
        }

        private void ActionButtonsMoreActionsButton_ButtonPressed(object sender, EventArgs e) => this.AddSampleActionButton();


        private void ActionButtonsToggleChange_ButtonPressed(object sender, EventArgs e) => this.AddSampleActionButton(true);

        private void ActionButtonsMoreSlotsButton_ButtonPressed(object sender, EventArgs e)
        {
            this.actionButtonsWindow.AvailableActionSlots++;
            this.UpdateActionButtonCounters();
        }

        private void ActionButtonsLessSlotsButton_ButtonPressed(object sender, EventArgs e)
        {
            this.actionButtonsWindow.AvailableActionSlots = Math.Max(2, this.actionButtonsWindow.AvailableActionSlots - 1);
            this.UpdateActionButtonCounters();
        }

        private void ActionButtonsCloseToggle_Toggled(object sender, EventArgs e) =>
            this.actionButtonsWindow.ShowCloseButton = !this.actionButtonsWindow.ShowCloseButton;

        private void ActionButtonsTogglePlacement_Toggled(object sender, EventArgs e) =>
            this.actionButtonsWindow.MoreActionsPlacement = this.actionButtonsTogglePlacement.IsOn
            ? MoreActionsButtonPlacement.BeforeFollowAndClose : MoreActionsButtonPlacement.BeforeActionButtons;

        private void ActionButtonsToggleBehavior_Toggled(object sender, EventArgs e) =>
            this.actionButtonsWindow.MoreActionsBehavior = this.actionButtonsToggleBehavior.IsOn
            ? MoreActionsPanelBehavior.HideAutomatically : MoreActionsPanelBehavior.StayOpen;

        private class AutomaticToggleChange : Behavior
        {
            private readonly TimeSpan toggleTime = TimeSpan.FromSeconds(2);
            private TimeSpan currentTime;

            [BindComponent(source: BindComponentSource.Children)]
            private ToggleStateManager stateManager = null;

            protected override void Update(TimeSpan gameTime)
            {
                if ((this.currentTime += gameTime) >= this.toggleTime)
                {
                    this.stateManager.ChangeState(this.stateManager.CurrentState.Value == ToggleState.On 
                        ? this.stateManager.States.ElementAt(0) : this.stateManager.States.ElementAt(1));
                    this.currentTime = TimeSpan.Zero;
                }
            }
        }
    }
}
