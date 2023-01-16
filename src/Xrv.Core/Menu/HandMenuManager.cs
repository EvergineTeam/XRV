// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features;
using Xrv.Core.Menu.Tutorial;

namespace Xrv.Core.Menu
{
    internal class HandMenuManager
    {
        private readonly EntityManager entityManager;
        private readonly AssetsService assetsService;
        private IPalmPanelBehavior palmPanelBehavior;

        private Entity menuEntity;

        public HandMenuManager(EntityManager entityManager, AssetsService assetsService)
        {
            this.entityManager = entityManager;
            this.assetsService = assetsService;
        }

        public HandMenu Initialize()
        {
            if (Tools.IsXRPlatformInputTrackingAvailable())
            {
                // Quest / Hololens
                this.palmPanelBehavior = new PalmPanelBehavior();
            }
            else
            {
                // Desktop
                this.palmPanelBehavior = new EmulatedPalmPanelBehavior();
            }

            this.palmPanelBehavior.DistanceFromHand = 0.1f;
            this.palmPanelBehavior.LookAtCameraUpperThreshold = 0.6f;
            this.palmPanelBehavior.OpenPalmUpperThreshold = 0.6f;

            var menuPrefab = this.GetMenuPrefab();
            this.menuEntity = menuPrefab.Instantiate();
            this.menuEntity.Name = "menuEntity";

            var palmMenuAnchorTransform = new Transform3D();
            var palmMenuAnchor = new Entity("palmMenuAnchor")
                .AddComponent(palmMenuAnchorTransform)
                .AddComponent(this.palmPanelBehavior as Component);

            var handMenu = new HandMenu();
            var menuRoot = new Entity("menuRoot")
                .AddComponent(new Transform3D())
                .AddComponent(new FollowPalmAnchor()
                {
                    SmoothTime = 0.06f,
                    Target = palmMenuAnchorTransform,
                })
                .AddComponent(handMenu)
                .AddComponent(new TutorialPresentation());

            menuRoot.AddChild(this.menuEntity);

            // On a first approach, palm menu seems to be rotated 180º.
            // To workaround this, we have placed menu entity as child of menu root, and roated it 180º.
            var menuEntityRotation = this.menuEntity.FindComponent<Transform3D>();
            menuEntityRotation.LocalRotation = new Vector3(0, MathHelper.Pi, 0);

            this.entityManager.Add(palmMenuAnchor);
            this.entityManager.Add(menuRoot);

            return handMenu;
        }

        private Prefab GetMenuPrefab() =>
            this.assetsService.Load<Prefab>(CoreResourcesIDs.Prefabs.HandMenu);
    }
}
