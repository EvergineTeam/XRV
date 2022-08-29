// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using System;

namespace Xrv.LoadModel
{

    /// <summary>
    /// Load model main behavior.
    /// </summary>
    public class LoadModelBehavior : Behavior
    {
        /// <summary>
        /// Loading effect entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_loading")]
        protected Entity Loading = null;

        /// <summary>
        /// Loading text 3d component.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_manipulator_loadingText")]
        protected Text3DMesh LoadingText = null;

        /// <summary>
        /// Locked icon entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.ChildrenSkipOwner, tag: "PART_manipulator_lockedIcon")]
        protected Entity LockedIcon = null;

        private Entity modelEntity;
        private AssetsService assetsService;

        /// <summary>
        /// Gets or sets the model loaded entity.
        /// </summary>
        public Entity ModelEntity
        {
            get => this.modelEntity;
            set
            {
                if (value != null)
                {
                    this.modelEntity = value;
                    this.AddManipulatorComponents(this.modelEntity);
                    this.Owner.AddChild(this.modelEntity);
                    this.Loading.IsEnabled = false;
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            var result = base.OnAttached();

            this.assetsService = Application.Current.Container.Resolve<AssetsService>();
            this.LockedIcon.IsEnabled = false;

            return result;
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
        }

        private void AddManipulatorComponents(Entity entity)
        {
            entity.AddComponent(new BoxCollider3D());
            entity.AddComponent(new StaticBody3D());
            entity.AddComponent(new Evergine.MRTK.SDK.Features.UX.Components.BoundingBox.BoundingBox()
            {
                AutoCalculate = false,
                BoxMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxVisual),
                BoxGrabbedMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxVisualGrabbed),
                ShowWireframe = true,
                ShowXRotationHandle = true,
                ShowYRotationHandle = true,
                ShowZRotationHandle = true,
                WireframeMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxWireframe),
                HandleMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxHandleBlue),
                HandleGrabbedMaterial = this.assetsService.Load<Material>(LoadModelResourceIDs.MRTK.Materials.BoundingBox.BoundingBoxHandleBlueGrabbed),
                RotationHandlePrefab = this.assetsService.Load<Prefab>(LoadModelResourceIDs.MRTK.Prefabs.BoundingBox_RotateHandle_weprefab),
            });
        }
    }
}
