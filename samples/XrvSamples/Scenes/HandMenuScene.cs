using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Diagnostics;
using System.Linq;
using Xrv.Core;
using Xrv.Core.Menu;

namespace XrvSamples.Scenes
{
    internal class HandMenuScene : BaseScene
    {
        private HandMenu handMenu;
        private Text3DMesh columnSize;
        private Text3DMesh numberOfButtons;
        private Text3DMesh detachStatus;
        private int counter;

        protected override void OnPostCreateXRScene()
        {
            var xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);
            this.handMenu = xrv.HandMenu;

            var increseColumnSize = this.Managers.EntityManager
                .FindAllByTag("increaseColumnSize")
                .First()
                .FindComponentInChildren<PressableButton>();
            increseColumnSize.ButtonReleased += this.IncreseColumnSize_ButtonReleased; 

            var decreaseColumnSize = this.Managers.EntityManager
                .FindAllByTag("decreaseColumnSize")
                .First()
                .FindComponentInChildren<PressableButton>();
            decreaseColumnSize.ButtonReleased += this.DecreaseColumnSize_ButtonReleased;

            var increaseNumberOfButtons = this.Managers.EntityManager
                .FindAllByTag("increaseNumberOfButtons")
                .First()
                .FindComponentInChildren<PressableButton>();
            increaseNumberOfButtons.ButtonReleased += this.IncreaseNumberOfButtons_ButtonReleased;

            var decreaseNumberOfButtons = this.Managers.EntityManager
                .FindAllByTag("decreaseNumberOfButtons")
                .First()
                .FindComponentInChildren<PressableButton>();
            decreaseNumberOfButtons.ButtonReleased += this.DecreaseNumberOfButtons_ButtonReleased;

            this.columnSize = this.Managers.EntityManager
                .FindAllByTag("columnSize")
                .First()
                .FindComponentInChildren<Text3DMesh>();

            this.numberOfButtons = this.Managers.EntityManager
                .FindAllByTag("numberOfButtons")
                .First()
                .FindComponentInChildren<Text3DMesh>();

            for (int i = 0; i < 6; i++)
            {
                this.AddButton();
            }

            this.detachStatus = this.Managers.EntityManager
                .FindAllByTag("detachStatus")
                .First()
                .FindComponentInChildren<Text3DMesh>();
            this.UpdateStatusInfo();
            this.handMenu.MenuStateChanged += this.HandMenu_MenuStateChanged;
        }

        private void IncreseColumnSize_ButtonReleased(object sender, EventArgs e)
        {
            this.handMenu.ButtonsPerColumn++;
            this.UpdateCounts();
        }

        private void DecreaseColumnSize_ButtonReleased(object sender, EventArgs e)
        {
            try
            {
                this.handMenu.ButtonsPerColumn--;
                this.UpdateCounts();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void IncreaseNumberOfButtons_ButtonReleased(object sender, EventArgs e) =>
            this.AddButton();

        private void DecreaseNumberOfButtons_ButtonReleased(object sender, EventArgs e)
        {
            if (this.handMenu.ButtonDescriptions.LastOrDefault() is MenuButtonDescription definition)
            {
                this.handMenu.ButtonDescriptions.Remove(definition);
                this.UpdateCounts();
            }
        }

        private void AddButton()
        {
            bool isToggle = DateTime.Now.Millisecond % 2 == 0;
            this.handMenu.ButtonDescriptions.Add(new MenuButtonDescription
            {
                IsToggle = isToggle,
                TextOn = $"{(isToggle ? "T_" : string.Empty)}{DateTime.Now.Millisecond}",
                IconOn = counter++ % 2 == 0
                    ? EvergineContent.Materials.Icons.Lambda
                    : EvergineContent.Materials.Icons.Pi,
            });
            this.UpdateCounts();
        }

        private void UpdateCounts()
        {
            this.columnSize.Text = this.handMenu.ButtonsPerColumn.ToString();
            this.numberOfButtons.Text = this.handMenu.ButtonDescriptions.Count.ToString();
        }

        private void HandMenu_MenuStateChanged(object sender, EventArgs e) => this.UpdateStatusInfo();

        private void UpdateStatusInfo() => this.detachStatus.Text = this.handMenu.IsDetached ? "Detached" : "Attached";
    }
}
