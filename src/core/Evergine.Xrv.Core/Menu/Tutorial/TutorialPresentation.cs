// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using System;
using System.Threading.Tasks;
using Evergine.Xrv.Core.UI.Windows;

namespace Evergine.Xrv.Core.Menu.Tutorial
{
    internal class TutorialPresentation : Component
    {
        [BindService]
        private AssetsService assetsService = null;

        [BindComponent]
        private HandMenu menu = null;

        private Entity handTutorialRootEntity;

        protected override void Start()
        {
            base.Start();

            if (this.menu.DisplayTutorial)
            {
                _ = this.CreateHandTutorialAsync();
            }

            this.IsEnabled = false;
        }

        private async Task CreateHandTutorialAsync()
        {
            await Task.Delay(1000);
            await EvergineBackgroundTask.Run(() =>
            {
                this.AddHandTutorialToScene();
            });
        }

        private void AddHandTutorialToScene()
        {
            // Load handtutorial model
            var handTutorialModel = this.assetsService.Load<Model>(CoreResourcesIDs.Models.Hand_Panel_anim_glb);
            var handTutorialEntity = handTutorialModel.InstantiateModelHierarchy(this.assetsService);
            handTutorialEntity.FindComponent<Transform3D>().Position = Vector3.Down * 0.2f;
            handTutorialEntity.AddComponent(new PingPongAnimation() { AnimationName = "Take 001" });
            var handMesh = handTutorialEntity.Find("[this].L_Hand.MeshL");
            handMesh.FindComponent<MaterialComponent>().Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.HandTutorial);
            handMesh.FindComponent<SkinnedMeshRenderer>().UseComputeSkinning = true;
            var panelMesh = handTutorialEntity.Find("[this].Panel");
            panelMesh.FindComponent<MaterialComponent>().Material = this.assetsService.Load<Material>(CoreResourcesIDs.Materials.SecondaryColor4);

            // In front of the camera
            var camera = this.Owner.Scene.Managers.RenderManager?.ActiveCamera3D;
            var intFrontCamera = Vector3.Zero;
            if (camera != null)
            {
                intFrontCamera = camera.Transform.Position + (camera.Transform.WorldTransform.Forward * 1.0f);
            }

            // Root with Tagalong
            this.handTutorialRootEntity = new Entity()
               .AddComponent(new Transform3D() { Position = intFrontCamera })
               .AddComponent(new WindowTagAlong()
               {
                   MaxHAngle = MathHelper.ToRadians(15),
                   MaxVAngle = MathHelper.ToRadians(45),
                   MaxLookAtAngle = MathHelper.ToRadians(15),
                   MinDistance = 0.6f,
                   MaxDistance = 0.8f,
                   DisableVerticalLookAt = false,
                   MaxVerticalDistance = 0.1f,
                   SmoothPositionFactor = 0.01f,
                   SmoothOrientationFactor = 0.05f,
                   SmoothDistanceFactor = 1.2f,
               });

            this.handTutorialRootEntity.AddChild(handTutorialEntity);
            this.menu.PalmUpDetected += this.HandMenu_PalmUpDetected;

            EvergineForegroundTask.Run(() =>
            {
                this.Owner.Scene.Managers.EntityManager.Add(this.handTutorialRootEntity);
            });
        }

        private void HandMenu_PalmUpDetected(object sender, EventArgs e)
        {
            this.handTutorialRootEntity.IsEnabled = false;
            this.menu.PalmUpDetected -= this.HandMenu_PalmUpDetected;
        }
    }
}
