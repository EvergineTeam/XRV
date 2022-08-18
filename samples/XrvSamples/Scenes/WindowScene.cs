using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using Xrv.Core;
using Xrv.Core.UI.Dialogs;
using Xrv.Core.UI.Windows;

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

        private PressableButton createAlert;
        private PressableButton createConfirm;
        private Text3DMesh dialogMessageText;

        private AssetsService assetsService;

        protected override void OnPostCreateXRScene()
        {
            var xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);

            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            this.windowsSystem = xrv.WindowSystem;
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
        }

        private void CreateAlert_ButtonReleased(object sender, EventArgs e)
        {
            var position = (sender as Component).Owner.FindComponent<Transform3D>().WorldTransform.Translation;
            var alertDialog = this.windowsSystem.ShowAlertDialog("test!", "this is a message\nSecond line", "ok", position + Vector3.UnitX * 0.3f);
            alertDialog.Closed += this.Dialog_Closed;
        }

        private void CreateConfirm_ButtonReleased(object sender, EventArgs e)
        {
            var position = (sender as Component).Owner.FindComponent<Transform3D>().WorldTransform.Translation;
            var confirmDialog = this.windowsSystem.ShowConfirmDialog("test!", "this is a message", "no", "yes", position + Vector3.UnitX * 0.3f);
            var configuration = confirmDialog.AcceptOption.Configuration;
            configuration.Plate = this.assetsService.Load<Material>(EvergineContent.XRV.Materials.Buttons.ButtonPrimary);
            confirmDialog.Closed += this.Dialog_Closed;
        }

        private void Dialog_Closed(object sender, EventArgs e)
        {
            if (sender is Dialog dialog)
            {
                dialog.Closed -= this.Dialog_Closed;
                this.dialogMessageText.Text = $"Dialog result: {dialog.Result ?? "<null>"}";
            }
        }

        private void ButtonWindow1_ButtonReleased(object sender, EventArgs e)
        {
            if (this.window1 == null)
            {
                this.window1 = this.windowsSystem.CreateWindow(new Vector3(0.2f, 0f, -0.6f));
                this.window1.Configuration.Title = "Window #1";
                this.window1.Configuration.Content = this.CreateText3D(
                    Text, 
                    new Vector2(0.3f, 0.2f),
                    new Vector3(0.01f, -0.01f, 0f));
                this.window1.Opened += this.Window1_Opened;
                this.window1.Closed += this.Window1_Closed;
            }

            if (this.window1.IsClosed)
            {
                this.window1.Open();
            }
            else
            {
                this.window1.Close();
            }
        }

        private void ButtonWindow2_ButtonReleased(object sender, EventArgs e)
        {
            if (this.window2 == null)
            {
                var assetsService = Application.Current.Container.Resolve<AssetsService>();
                this.window2 = this.windowsSystem.CreateWindow(new Vector3(0.6f, 0f, -0.65f));
                this.window2.Configuration.Title = "Window #2";
                this.window2.Configuration.Size = new Vector2(0.2f, 0.3f);
                ((WindowConfiguration)this.window2.Configuration).LogoMaterial = assetsService.Load<Material>(EvergineContent.Materials.EvergineLogo);
                this.window2.Configuration.FrontPlateSize = new Vector2(0.2f, 0.25f);
                this.window2.Configuration.FrontPlateOffsets = new Vector2(0f, 0.025f);
                this.window2.Configuration.Content = this.CreateText3D(
                    Text, 
                    new Vector2(0.18f, 0.25f),
                    new Vector3(0f, 0.01f, 0f));
                this.window2.Opened += this.Window2_Opened;
                this.window2.Closed += this.Window2_Closed;
            }

            if (this.window2.IsClosed)
            {
                this.window2.Open();
            }
            else
            {
                this.window2.Close();
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
    }
}
