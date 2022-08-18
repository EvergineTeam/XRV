using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Threading;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xrv.Core;
using Xrv.Core.Menu;

namespace XrvSamples.Scenes
{
    internal class HandMenuScene : BaseScene
    {
        private HandMenu handMenu;
        private Text3DMesh columnSize;
        private Text3DMesh numberOfButtons;
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
        }

        private void IncreseColumnSize_ButtonReleased(object sender, EventArgs e)
        {
            this.handMenu.ButtonsPerColumn++;
            this.UpdateCounts();
        }

        private void DecreaseColumnSize_ButtonReleased(object sender, EventArgs e)
        {
            this.handMenu.ButtonsPerColumn--;
            this.UpdateCounts();
        }

        private void IncreaseNumberOfButtons_ButtonReleased(object sender, EventArgs e) =>
            this.AddButton();

        private void DecreaseNumberOfButtons_ButtonReleased(object sender, EventArgs e)
        {
            if (this.handMenu.ButtonDefinitions.LastOrDefault() is HandMenuButtonDescription definition)
            {
                this.handMenu.ButtonDefinitions.Remove(definition);
                this.UpdateCounts();
            }
        }

        private void AddButton()
        {
            bool isToggle = DateTime.Now.Millisecond % 2 == 0;
            this.handMenu.ButtonDefinitions.Add(new HandMenuButtonDescription
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
            this.numberOfButtons.Text = this.handMenu.ButtonDefinitions.Count.ToString();
        }
    }
}
