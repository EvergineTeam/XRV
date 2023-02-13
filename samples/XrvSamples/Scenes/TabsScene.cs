using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using Evergine.Xrv.Core;
using Evergine.Xrv.Core.UI.Tabs;
using Evergine.Xrv.Core.UI.Windows;

namespace XrvSamples.Scenes
{
    internal class TabsScene : BaseScene
    {
        private Text3DMesh numberOfButtons;
        private TabControl tabControl;

        public override void RegisterManagers()
        {
            base.RegisterManagers();
            Managers.AddManager(new Evergine.Bullet.BulletPhysicManager3D());
        }

        protected override void OnPostCreateXRScene()
        {
            var xrv = Application.Current.Container.Resolve<XrvService>();
            xrv.Initialize(this);

            var increaseNumberOfButtons = this.Managers.EntityManager
                .FindAllByTag("increaseNumber")
                .First()
                .FindComponentInChildren<PressableButton>();
            increaseNumberOfButtons.ButtonReleased += this.IncreaseNumberOfItems_ButtonReleased;

            var decreaseNumberOfButtons = this.Managers.EntityManager
                .FindAllByTag("decreaseNumber")
                .First()
                .FindComponentInChildren<PressableButton>();
            decreaseNumberOfButtons.ButtonReleased += this.DecreaseNumberOfItems_ButtonReleased;

            this.numberOfButtons = this.Managers.EntityManager
                .FindAllByTag("numberOfButtons")
                .First()
                .FindComponentInChildren<Text3DMesh>();

            var window = xrv.WindowsSystem.CreateWindow(config =>
            {
                var tabEntity = TabControl.Builder
                    .Create()
                    .WithSize(new Vector2(0.315f, 0.2f))
                    .AddItem(new TabItem
                    {
                        Name = () => "Repositories",
                        Contents = () => this.CreateText(0),
                    })
                    .Build();
                this.tabControl = tabEntity.FindComponent<TabControl>();
                this.UpdateCounts();
                config.Content = tabEntity;
                config.DisplayFrontPlate = false;
            });

            EvergineForegroundTask.Run(window.Open);
        }

        private void IncreaseNumberOfItems_ButtonReleased(object sender, EventArgs e)
        {
            var count = this.tabControl.Items.Count;
            this.tabControl.Items.Add(new TabItem
            {
                Name = () => $"Item #{count}",
                Data = count,
                Contents = () => this.CreateText(count),
            });
            this.UpdateCounts();
        }

        private void DecreaseNumberOfItems_ButtonReleased(object sender, EventArgs e)
        {
            if (this.tabControl.Items.Count > 0)
            {
                this.tabControl.Items.RemoveAt(this.tabControl.Items.Count - 1);
                this.UpdateCounts();
            }
        }

        private void UpdateCounts() => this.numberOfButtons.Text = this.tabControl.Items.Count.ToString();

        private Entity CreateText(int value) =>
            new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new Text3DMesh
                {
                    Text = $"This is item #{value}",
                    ScaleFactor = 0.02f,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Origin = new Vector2(0.5f, 0.5f),
                })
                .AddComponent(new Text3DRenderer());
    }
}
