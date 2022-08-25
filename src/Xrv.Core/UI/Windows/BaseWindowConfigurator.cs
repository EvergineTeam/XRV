﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Mathematics;
using System.Linq;
using Xrv.Core.UI.Buttons;

namespace Xrv.Core.UI.Windows
{
    public abstract class BaseWindowConfigurator : Component
    {
        private Vector2 size = new Vector2(0.35f, 0.3f);
        private Vector2 frontPlateOffsets = new Vector2(0f);

        private Vector2 frontPlateSize;
        private string title;
        private Entity content;

        private bool displayFrontPlate = true;
        private bool displayLogo = true;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_back_plate")]
        protected PlaneMesh backPlate;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_header_plate")]
        protected PlaneMesh headerPlate;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_header_plate")]
        protected Transform3D headerTransform;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_front_plate")]
        protected PlaneMesh frontPlate;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_front_plate")]
        protected Transform3D frontTransform;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_text")]
        protected Text3DMesh titleMesh;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_text_container")]
        protected Transform3D titleTextContainerTransform;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_button_container")]
        protected Transform3D titleButtonContainerTransform;

        [BindComponent(isRequired: false)]
        protected BoxCollider3D manipulationCollider;

        protected Entity logoEntity;
        protected Entity contentEntity;

        public Entity Content
        {
            get => this.content;
            set
            {
                this.content = value;
                if (this.IsAttached)
                {
                    this.UpdateContent();
                }
            }
        }

        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
                if (this.IsAttached)
                {
                    this.UpdateTitle();
                }
            }
        }

        public Vector2 Size
        {
            get => this.size;
            set
            {
                this.size = value;
                if (this.IsAttached)
                {
                    this.UpdateSize();
                }
            }
        }

        public Vector2 FrontPlateOffsets
        {
            get => this.frontPlateOffsets;
            set
            {
                this.frontPlateOffsets = value;
                if (this.IsAttached)
                {
                    this.UpdateFrontPlateOffsets();
                }
            }
        }

        public Vector2 FrontPlateSize
        {
            get => this.frontPlateSize;
            set
            {
                this.frontPlateSize = value;
                if (this.IsAttached)
                {
                    this.UpdateFrontPlateSize();
                }
            }
        }

        public bool DisplayFrontPlate
        {
            get => this.displayFrontPlate;

            set
            {
                if (this.displayFrontPlate != value)
                {
                    this.displayFrontPlate = value;
                    this.UpdateDisplayFrontPlate();
                }
            }
        }

        public bool DisplayLogo
        {
            get => this.displayLogo;

            set
            {
                if (this.displayLogo != value)
                {
                    this.displayLogo = value;
                    this.UpdateDisplayLogo();
                }
            }
        }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.contentEntity = this.Owner.FindChildrenByTag("PART_window_content", isRecursive: true).First();
                this.logoEntity = this.Owner.FindChildrenByTag("PART_window_logo", isRecursive: true).First();
            }

            return attached;
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            this.UpdateSize();
            this.UpdateFrontPlateSize();
            this.UpdateFrontPlateOffsets();
            this.UpdateContent();
            this.UpdateTitle();
            this.UpdateDisplayLogo();
            this.UpdateDisplayFrontPlate();
        }

        protected virtual void UpdateFrontPlateSize()
        {
            if (!this.CheckCorrectSizes())
            {
                return;
            }

            this.frontPlate.Width = this.frontPlateSize.X;
            this.frontPlate.Height = this.frontPlateSize.Y;
        }

        protected virtual void UpdateFrontPlateOffsets()
        {
            var frontOffset = this.frontTransform.LocalPosition;
            frontOffset.X = this.frontPlateOffsets.X;
            frontOffset.Y = this.frontPlateOffsets.Y;
            this.frontTransform.LocalPosition = frontOffset;
        }

        protected virtual void UpdateSize()
        {
            if (!this.CheckCorrectSizes())
            {
                return;
            }

            this.backPlate.Width = this.size.X;
            this.backPlate.Height = this.size.Y;

            this.headerPlate.Width = this.size.X;

            var halfSize = this.size * 0.5f;
            var headerTransform = this.headerTransform.LocalPosition;
            headerTransform.Y = halfSize.Y + 0.018f;
            this.headerTransform.LocalPosition = headerTransform;

            var titleText = this.titleTextContainerTransform.LocalPosition;
            titleText.X = -halfSize.X;
            this.titleTextContainerTransform.LocalPosition = titleText;

            var buttonContainer = this.titleButtonContainerTransform.LocalPosition;
            buttonContainer.X = halfSize.X;
            this.titleButtonContainerTransform.LocalPosition = buttonContainer;

            if (this.manipulationCollider != null)
            {
                var colliderSize = this.manipulationCollider.Size;
                colliderSize.X = this.size.X;
                colliderSize.Y = this.size.Y + ButtonConstants.SquareButtonSize;
                this.manipulationCollider.Size = colliderSize;

                var colliderOffset = this.manipulationCollider.Offset;
                colliderOffset.Y = ButtonConstants.SquareButtonSize / 2;
                this.manipulationCollider.Offset = colliderOffset;
            }
        }

        private bool CheckCorrectSizes() =>
            this.size.X != 0
            && this.size.Y != 0
            && this.frontPlateSize.X != 0
            && this.frontPlateSize.Y != 0;

        private void UpdateTitle() => this.titleMesh.Text = this.title;

        private void UpdateContent()
        {
            if (this.contentEntity == null)
            {
                return;
            }

            foreach (var item in this.contentEntity.ChildEntities.ToList())
            {
                if (item.Tag == "PART_window_front_plate")
                {
                    continue;
                }

                if (this.content != null && this.content.Id != item.Id)
                {
                    this.contentEntity.RemoveChild(item);
                }
            }

            if (this.content != null
                && this.contentEntity.ChildEntities.Count() == 1)
            {
                this.contentEntity.AddChild(this.content);
            }
        }

        private void UpdateDisplayLogo()
        {
            if (this.IsAttached)
            {
                this.logoEntity.IsEnabled = this.displayLogo;
            }
        }

        private void UpdateDisplayFrontPlate()
        {
            if (this.IsAttached)
            {
                this.frontPlate.Owner.IsEnabled = this.displayFrontPlate;
            }
        }
    }
}
