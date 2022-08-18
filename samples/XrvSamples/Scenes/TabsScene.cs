using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Linq;
using Xrv.Core;
using Xrv.Core.UI.Tabs;
using Xrv.Core.UI.Windows;

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

            var window = this.Managers.EntityManager
                .FindAllByTag("window")
                .First()
                .FindComponentInChildren<Window>();
            var tabEntity = TabControl.Builder
                .Create()
                .WithSize(new Vector2(0.286f, 0.2f))
                .AddItem(new TabItem
                {
                    Text = "Repositories",
                })
                .Build();
            this.tabControl = tabEntity.FindComponent<TabControl>();
            this.tabControl.SelectedItemChanged += this.TabControl_SelectedItemChanged;
            this.UpdateCounts();

            EvergineForegroundTask.Run(() =>
            {
                var transform = tabEntity.FindComponent<Transform3D>();
                var position = transform.LocalPosition;
                position.X += 0.034f;
                transform.LocalPosition = position;
                window.Configuration.Content = tabEntity;
                window.Configuration.DisplayFrontPlate = false;
            });
        }

        private void IncreaseNumberOfItems_ButtonReleased(object sender, EventArgs e)
        {
            var count = this.tabControl.Items.Count;
            this.tabControl.Items.Add(new TabItem
            {
                Text = $"Item #{count}",
                Data = count,
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

        private void TabControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs args)
        {
            if (args.Item?.Data is int @value)
            {
                var textEntity = this.CreateText(@value);
                this.tabControl.Content = textEntity;
            }
            else
            {
                this.tabControl.Content = null;
            }
        }

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
