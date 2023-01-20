// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using System;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Assigns material to painter cursor indicator. Please, consider that current cursor
    /// has a sphere and several cylinders, which should share same material.
    /// We have two independent hands, each one should show its
    /// own cursors. This is important because we need two different instances for material,
    /// as each one of the cursos changes in opacity and color depending on user actions.
    /// This component forces a new material instance to make have independent materials for
    /// both cursors.
    /// </summary>
    public class CursorMaterialAssignation : Component
    {
        private Material material;
        private Material clonedMaterial;

        /// <summary>
        /// Raised when material has been updated.
        /// </summary>
        public event EventHandler MaterialUpdated;

        /// <summary>
        /// Gets or sets cursor target material.
        /// </summary>
        public Material Material
        {
            get => this.material;
            set
            {
                if (this.material != value)
                {
                    this.material = value;

                    if (this.IsAttached)
                    {
                        this.OnMaterialUpdate();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.OnMaterialUpdate();
            }

            return attached;
        }

        private void OnMaterialUpdate()
        {
            this.clonedMaterial?.Dispose();
            this.clonedMaterial = this.Managers.AssetSceneManager.Load<Material>(this.material.Id, true);

            var descendantMaterials = this.Owner.FindComponentsInChildren<MaterialComponent>(skipOwner: true);
            if (descendantMaterials != null)
            {
                foreach (var descendant in descendantMaterials)
                {
                    descendant.Material = this.clonedMaterial;
                }
            }

            this.MaterialUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}
