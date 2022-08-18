using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework.XR;
using Evergine.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Xrv.Core.Menu
{
    public class HandMenu : Component
    {
        private const float ButtonWidth = 0.032f;

        private readonly ObservableCollection<HandMenuButtonDescription> buttonDefinitions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private int buttonsPerColumn = 4;

        [BindComponent(isExactType: false, source: BindComponentSource.Scene)]
        private IPalmPanelBehavior palmPanelBehavior = null;

        [BindService()]
        private AssetsService assetsService = null;

        [BindService()]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_back_plate")]
        private PlaneMesh backPlateMesh = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_back_plate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_front_plate")]
        private PlaneMesh frontPlateMesh = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_front_plate")]
        private Transform3D frontPlateTransform = null;

        private Entity buttonsContainer = null;

        public HandMenu()
        {
            this.buttonDefinitions = new ObservableCollection<HandMenuButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        public int ButtonsPerColumn
        {
            get => this.buttonsPerColumn;

            set
            {
                if (this.buttonsPerColumn != value && value > 0)
                {
                    this.buttonsPerColumn = value;
                    this.ReorderButtons();
                }
            }
        }

        public IList<HandMenuButtonDescription> ButtonDefinitions { get => this.buttonDefinitions; }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.buttonsContainer = this.Owner.FindChildrenByTag("PART_hand_menu_buttons_container").First();
                this.InternalAddButtons(this.buttonDefinitions); // We can have items added before this component has been attached
                this.buttonDefinitions.CollectionChanged += this.ButtonDefinitions_CollectionChanged;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            this.buttonDefinitions.Clear();
            this.buttonDefinitions.CollectionChanged -= this.ButtonDefinitions_CollectionChanged;
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.palmPanelBehavior != null)
            {
                this.palmPanelBehavior.PalmUpChanged += this.PalmPanelBehavior_PalmUpChanged;
                this.palmPanelBehavior.ActiveHandednessChanged += this.PalmPanelBehavior_ActiveHandednessChanged;
                this.RefreshMenuState();
            }

            this.ReorderButtons();
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.palmPanelBehavior != null)
            {
                this.palmPanelBehavior.PalmUpChanged -= this.PalmPanelBehavior_PalmUpChanged;
                this.palmPanelBehavior.ActiveHandednessChanged -= this.PalmPanelBehavior_ActiveHandednessChanged;
            }
        }

        private void PalmPanelBehavior_ActiveHandednessChanged(object sender, XRHandedness hand)
        {
            this.RefreshMenuState();
        }

        private void PalmPanelBehavior_PalmUpChanged(object sender, bool palmUp)
        {
            this.RefreshMenuState();
        }

        private void RefreshMenuState()
        {
            // Perform animation here?
        }

        private void ButtonDefinitions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.InternalAddButtons(args.NewItems.OfType<HandMenuButtonDescription>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.InternalRemoveButtons(args.OldItems.OfType<HandMenuButtonDescription>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.InternalRemoveButtons(this.buttonDefinitions);
                    break;
            }

            this.ReorderButtons();
        }

        private void InternalAddButtons(IEnumerable<HandMenuButtonDescription> buttons)
        {
            var buttonsFactory = new HandMenuButtonFactory(this.xrvService, this.assetsService);

            foreach (var definition in buttons)
            {
                var buttonInstance = buttonsFactory.CreateInstance(definition);
                // MRTK buttons look to negative Z, so we have to invert this component
                var buttonTransform = buttonInstance.FindComponent<Transform3D>();
                buttonTransform.LocalRotation = new Vector3(0f, MathHelper.Pi, 0);

                this.buttonsContainer.AddChild(buttonInstance);
                this.instantiatedButtons.Add(definition.Id, buttonInstance);
            }
        }

        private void InternalRemoveButtons(IEnumerable<HandMenuButtonDescription> buttons)
        {
            var entityManager = this.Managers.EntityManager;

            foreach (var definition in buttons)
            {
                if (this.instantiatedButtons.ContainsKey(definition.Id))
                {
                    var buttonToRemove = this.instantiatedButtons[definition.Id];
                    entityManager.Remove(buttonToRemove);
                    this.instantiatedButtons.Remove(definition.Id);
                }
            }
        }

        private void ReorderButtons()
        {
            int numberOfColumns = (int)Math.Ceiling(this.buttonDefinitions.Count / (float)this.buttonsPerColumn);
            float menuWidth = Math.Max(0.0001f, numberOfColumns * ButtonWidth); // to avoid zero width exception

            // Resize back plate
            this.backPlateMesh.Width = menuWidth;
            this.backPlateMesh.Height = (this.buttonsPerColumn + 1) * ButtonWidth;

            // Resize front plate
            this.frontPlateMesh.Width = menuWidth;
            this.frontPlateMesh.Height = this.buttonsPerColumn * ButtonWidth;

            // Panels translation (affected when adding columns, origin is not in top-left corner)
            var transform = this.backPlateTransform;
            var padding = (numberOfColumns - 1) * (ButtonWidth / 2);
            var position = transform.LocalPosition;
            position.X = padding;
            transform.LocalPosition = position;
            transform = this.frontPlateTransform;
            position = transform.LocalPosition;
            position.X = padding;
            transform.LocalPosition = position;

            float rowInitialY = (this.buttonsPerColumn - 2) * (ButtonWidth / 2);

            // Add buttons
            for (int i = 0; i < this.buttonDefinitions.Count; i++)
            {
                HandMenuButtonDescription definition = this.buttonDefinitions[i];
                Entity button = this.instantiatedButtons[definition.Id];
                var buttonTransform = button.FindComponent<Transform3D>();
                var buttonPosition = buttonTransform.LocalPosition;
                buttonPosition.X = ButtonWidth * (i / this.buttonsPerColumn);
                buttonPosition.Y = rowInitialY - ((i % this.buttonsPerColumn) * ButtonWidth);
                buttonTransform.LocalPosition = buttonPosition;
            }
        }
    }
}
