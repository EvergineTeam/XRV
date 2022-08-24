using Evergine.Common.Input.Keyboard;
using Evergine.Common.Input;
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
using Xrv.Core.Extensions;
using Evergine.Components.WorkActions;
using Evergine.Common.Graphics;
using Evergine.Components.Fonts;
using System.Runtime.CompilerServices;

namespace Xrv.Core.Menu
{
    public class HandMenu : Behavior
    {
        [BindService]
        protected GraphicsPresenter graphicsPresenter;

        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<HandMenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private int maxButtonsPerColumn = 4;

        [BindComponent(isExactType: false, source: BindComponentSource.Scene)]
        private IPalmPanelBehavior palmPanelBehavior = null;

        [BindService()]
        private AssetsService assetsService = null;

        [BindService()]
        private XrvService xrvService = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_back_plate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.Children, tag: "PART_hand_menu_front_plate")]
        private Transform3D frontPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_detach")]
        protected Transform3D detachButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_follow")]
        protected Transform3D followButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        protected Transform3D textTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        protected Text3DMesh text3DMesh = null;

        private Entity buttonsContainer = null;
        private IWorkAction animation;
        private int numberOfButtons;
        private int numberOfColumns;
        private int numberButtonsPerColumns;

        public HandMenu()
        {
            this.buttonDescriptions = new ObservableCollection<HandMenuButtonDescription>();
            this.instantiatedButtons = new Dictionary<Guid, Entity>();
        }

        public int ButtonsPerColumn
        {
            get => this.maxButtonsPerColumn;

            set
            {
                if (this.maxButtonsPerColumn != value && value > 0)
                {
                    this.maxButtonsPerColumn = value;
                    this.ReorderButtons();
                }
            }
        }

        public IList<HandMenuButtonDescription> ButtonDescriptions { get => this.buttonDescriptions; }

        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.buttonsContainer = this.Owner.FindChildrenByTag("PART_hand_menu_buttons_container").First();
                this.InternalAddButtons(this.buttonDescriptions); // We can have items added before this component has been attached
                this.buttonDescriptions.CollectionChanged += this.ButtonDefinitions_CollectionChanged;
            }

            return attached;
        }

        protected override void OnDetach()
        {
            this.buttonDescriptions.Clear();
            this.buttonDescriptions.CollectionChanged -= this.ButtonDefinitions_CollectionChanged;
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

            this.followButtonTransform.Owner.IsEnabled = false;
            this.text3DMesh.Owner.IsEnabled = false;

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
                    this.InternalClearButtons();
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
                Workarounds.MrtkRotateButton(buttonInstance);
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

        private void InternalClearButtons()
        {
            this.instantiatedButtons.Clear();
            this.buttonsContainer.RemoveAllChildren();
        }

        private void ReorderButtons()
        {
            this.numberOfButtons = this.buttonDescriptions.Count;
            this.numberOfColumns = (int)Math.Ceiling(this.buttonDescriptions.Count / (float)this.maxButtonsPerColumn);
            this.numberButtonsPerColumns = this.buttonDescriptions.Count < this.maxButtonsPerColumn ? Math.Max(this.buttonDescriptions.Count, 4) : this.maxButtonsPerColumn;

            // Resize back plate
            this.backPlateTransform.LocalScale = new Vector3(this.numberOfColumns, 1, 1);

            // Resize front plate
            this.frontPlateTransform.LocalScale = new Vector3(this.numberOfColumns, this.numberButtonsPerColumns, 1);

            // Add buttons
            for (int i = 0; i < this.buttonDescriptions.Count; i++)
            {
                HandMenuButtonDescription definition = this.buttonDescriptions[i];
                Entity button = this.instantiatedButtons[definition.Id];
                var buttonTransform = button.FindComponent<Transform3D>();

                var buttonPosition = buttonTransform.LocalPosition;
                buttonPosition.X = ButtonWidthOverTwo + (ButtonWidth * (i / this.numberButtonsPerColumns));
                buttonPosition.Y = -ButtonWidthOverTwo - ((i % this.numberButtonsPerColumns) * ButtonWidth);
                buttonTransform.LocalPosition = buttonPosition;
            }
        }

        protected override void Update(TimeSpan gameTime)
        {
            KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

            if (keyboardDispatcher?.ReadKeyState(Keys.K) == ButtonState.Pressing)
            {
                this.ConvertTo(true);
            }
            else if (keyboardDispatcher?.ReadKeyState(Keys.J) == ButtonState.Pressing)
            {
                this.ConvertTo(false);
            }
        }

        private void ConvertTo(bool windowMode)
        {
            float start = windowMode ? 0 : 1;
            float end = windowMode ? 1 : 0;

            this.animation?.Cancel();
            this.animation = new ActionWorkAction(() =>
            {
                if (end == 1)
                {
                    this.text3DMesh.Owner.IsEnabled = true;
                    this.followButtonTransform.Owner.IsEnabled = true;
                }
            })
            .ContinueWith(
                new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, (f) =>
                {
                    // Front and back plates animation
                    this.frontPlateTransform.Scale = Vector3.Lerp(new Vector3(this.numberOfColumns, this.numberButtonsPerColumns, 1), new Vector3(this.numberButtonsPerColumns, 1, 1), f);
                    this.backPlateTransform.Scale = Vector3.Lerp(Vector3.One, new Vector3(this.numberButtonsPerColumns, 1, 1), f);

                    // Header animation
                    this.detachButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(ButtonWidthOverTwo, 0, 0.003f), new Vector3(ButtonWidthOverTwo + (ButtonWidth * (this.numberButtonsPerColumns - 1)), 0, 0.003f), f);
                    this.followButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidthOverTwo, 0, 0.003f), new Vector3(-ButtonWidthOverTwo + (ButtonWidth * (this.numberButtonsPerColumns - 1)), 0, 0.003f), f);

                    this.textTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidth * 2, 0, 0.03f), new Vector3(0.015f, 0, 0.03f), f);
                    this.text3DMesh.Color = Color.Lerp(Color.Transparent, Color.White, f);

                    // Buttons animation
                    int i = 0;
                    foreach (var button in this.instantiatedButtons.Values)
                    {
                        var buttonTransform = button.FindComponent<Transform3D>();
                        buttonTransform.LocalPosition = Vector3.Lerp(new Vector3(ButtonWidthOverTwo + (ButtonWidth * (i / this.numberButtonsPerColumns)), -ButtonWidthOverTwo - ((i % this.numberButtonsPerColumns) * ButtonWidth), 0),
                                                                     new Vector3(ButtonWidthOverTwo + ((i % this.numberButtonsPerColumns) * ButtonWidth), -ButtonWidthOverTwo - (ButtonWidth * (i / this.numberButtonsPerColumns)), 0),
                                                                        f);

                        i++;
                    }
                })
            ).ContinueWith(
                new ActionWorkAction(() =>
                {
                    if (end == 0)
                    {
                        this.text3DMesh.Owner.IsEnabled = false;
                        this.followButtonTransform.Owner.IsEnabled = false;
                    }
                }
            )
            );
            this.animation.Run();
        }
    }
}
