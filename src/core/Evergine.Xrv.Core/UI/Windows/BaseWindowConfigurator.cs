// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Components.Fonts;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Physics3D;
using Evergine.Mathematics;
using System;
using System.Linq;
using Evergine.Xrv.Core.Localization;
using Evergine.Xrv.Core.UI.Buttons;

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Base configurator component for windows.
    /// </summary>
    public abstract class BaseWindowConfigurator : Component
    {
        /// <summary>
        /// Back plate mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_back_plate")]
        protected PlaneMesh backPlate;

        /// <summary>
        /// Header plate mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_header_plate")]
        protected PlaneMesh headerPlate;

        /// <summary>
        /// Header plate transform.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_header_plate")]
        protected Transform3D headerTransform;

        /// <summary>
        /// Front plate mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_front_plate")]
        protected PlaneMesh frontPlate;

        /// <summary>
        /// Front plate transform.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_front_plate")]
        protected Transform3D frontTransform;

        /// <summary>
        /// Title text mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_text")]
        protected Text3DMesh titleMesh;

        /// <summary>
        /// Title text mesh.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_text")]
        protected Text3dLocalization titleLocalization;

        /// <summary>
        /// Title text container transform.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_text_container")]
        protected Transform3D titleTextContainerTransform;

        /// <summary>
        /// Title button container transform.
        /// </summary>
        [BindComponent(source: BindComponentSource.Children, tag: "PART_window_title_button_container")]
        protected Transform3D titleButtonContainerTransform;

        /// <summary>
        /// Box collider for window manipulation.
        /// </summary>
        [BindComponent(isRequired: false)]
        protected BoxCollider3D manipulationCollider;

        private Vector2 size = new Vector2(0.35f, 0.3f);
        private Vector2 frontPlateOffsets = new Vector2(0f);

        private Vector2 frontPlateSize;
        private string title;
        private Func<string> localizedTitle;
        private Entity logoEntity;
        private Entity contentEntity;
        private Entity content;

        private bool displayBackPlate = true;
        private bool displayFrontPlate = true;
        private bool displayLogo = true;

        /// <summary>
        /// Gets or sets window contents.
        /// </summary>
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

        /// <summary>
        /// Gets or sets window title.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a function to retrieve localized window title.
        /// </summary>
        [IgnoreEvergine]
        public Func<string> LocalizedTitle
        {
            get => this.titleLocalization != null ? this.titleLocalization.LocalizationFunc : this.localizedTitle;

            set
            {
                if (this.titleLocalization != null)
                {
                    this.titleLocalization.LocalizationFunc = value;
                }

                this.localizedTitle = value;

                if (this.IsAttached)
                {
                    this.UpdateTitle();
                }
            }
        }

        /// <summary>
        /// Gets or sets window size.
        /// </summary>
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

        /// <summary>
        /// Gets or sets front plate XY plane offset relative to back plate.
        /// </summary>
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

        /// <summary>
        /// Gets or sets front plate XY size.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether front plate should be displayed or not.
        /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether back plate should be displayed or not.
        /// </summary>
        public bool DisplayBackPlate
        {
            get => this.displayBackPlate;

            set
            {
                if (this.displayBackPlate != value)
                {
                    this.displayBackPlate = value;
                    this.UpdateDisplayBackPlate();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether bottom left icon should be displayed or not.
        /// </summary>
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

        internal void UpdateContent()
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

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.contentEntity = this.Owner.FindChildrenByTag("PART_window_content", isRecursive: true).First();
                this.logoEntity = this.Owner.FindChildrenByTag("PART_window_logo", isRecursive: true).First();

                if (this.localizedTitle != null && this.titleLocalization != null)
                {
                    this.titleLocalization.LocalizationFunc = this.localizedTitle;
                    this.localizedTitle = null;
                }
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            this.UpdateSize();
            this.UpdateFrontPlateSize();
            this.UpdateFrontPlateOffsets();
            this.UpdateContent();
            this.UpdateTitle();
            this.UpdateDisplayLogo();
            this.UpdateDisplayBackPlate();
            this.UpdateDisplayFrontPlate();
        }

        /// <summary>
        /// Updates front plate size.
        /// </summary>
        protected virtual void UpdateFrontPlateSize()
        {
            if (!this.CheckCorrectSizes())
            {
                return;
            }

            this.frontPlate.Width = this.frontPlateSize.X;
            this.frontPlate.Height = this.frontPlateSize.Y;
        }

        /// <summary>
        /// Updates front plate offsets.
        /// </summary>
        protected virtual void UpdateFrontPlateOffsets()
        {
            var frontOffset = this.frontTransform.LocalPosition;
            frontOffset.X = this.frontPlateOffsets.X;
            frontOffset.Y = this.frontPlateOffsets.Y;
            this.frontTransform.LocalPosition = frontOffset;
        }

        /// <summary>
        /// Updates window size.
        /// </summary>
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

        private void UpdateTitle() =>
            this.titleMesh.Text = this.localizedTitle != null
                ? this.localizedTitle.Invoke()
                : this.title;

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

        private void UpdateDisplayBackPlate()
        {
            if (this.IsAttached)
            {
                this.backPlate.Owner.IsEnabled = this.displayBackPlate;
            }
        }
    }
}
