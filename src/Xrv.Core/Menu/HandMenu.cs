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
using Evergine.MRTK.SDK.Features.UX.Components.ToggleButtons;
using System.Reflection;

namespace Xrv.Core.Menu
{
    public class HandMenu : Behavior
    {
        private const float ButtonWidth = 0.032f;
        private const float ButtonWidthOverTwo = ButtonWidth * 0.5f;

        private readonly ObservableCollection<HandMenuButtonDescription> buttonDescriptions;
        private readonly Dictionary<Guid, Entity> instantiatedButtons;

        private int maxButtonsPerColumn = 4;

        [BindService]
        private AssetsService assetsService = null;

        [BindService]
        private XrvService xrvService = null;

        [BindComponent(isExactType: false, source: BindComponentSource.Scene)]
        private IPalmPanelBehavior palmPanelBehavior = null;

        [BindComponent]
        protected Transform3D handMenuTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_back_plate")]
        private Transform3D backPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_front_plate")]
        private Transform3D frontPlateTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_detach")]
        protected Transform3D detachButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_detach")]
        protected ToggleButton detachButtonToggle = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_follow")]
        protected Transform3D followButtonTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        protected Transform3D textTransform = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "PART_hand_menu_text")]
        protected Text3DMesh text3DMesh = null;

        private Entity buttonsContainer = null;
        private IWorkAction appearAnimation;
        private IWorkAction extendedAnimation;
        private int numberOfButtons;
        private int numberOfColumns;
        private int numberButtonsPerColumns;

        protected bool menuExtended = false;

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
            }

            this.detachButtonToggle.Toggled += this.DetachButtonToggle_Toggled;

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

            this.detachButtonToggle.Toggled -= this.DetachButtonToggle_Toggled;
        }

        private void PalmPanelBehavior_ActiveHandednessChanged(object sender, XRHandedness hand)
        {
            this.currentHand = hand;
            this.AppearAnimation(true);
        }

        private void PalmPanelBehavior_PalmUpChanged(object sender, bool palmUp)
        {
            this.AppearAnimation(!palmUp);
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
            this.numberOfColumns = (int)Math.Ceiling(numberOfButtons / (float)this.maxButtonsPerColumn);
            this.numberButtonsPerColumns = numberOfButtons < this.maxButtonsPerColumn ? Math.Max(numberOfButtons, 4) : this.maxButtonsPerColumn;

            // Resize back plate
            this.backPlateTransform.LocalScale = new Vector3(this.numberOfColumns, 1, 1);

            // Resize front plate
            this.frontPlateTransform.LocalScale = new Vector3(this.numberOfColumns, this.numberButtonsPerColumns, 1);

            // Add buttons
            int i = 0;
            foreach (var button in this.instantiatedButtons.Values.Reverse())
            {
                var buttonTransform = button.FindComponent<Transform3D>();
                buttonTransform.LocalPosition = new Vector3(ButtonWidthOverTwo + (ButtonWidth * (i / this.numberButtonsPerColumns)), -ButtonWidthOverTwo - ((i % this.numberButtonsPerColumns) * ButtonWidth), 0);
                i++;
            }
        }

        private void AppearAnimation(bool show)
        {
            float start = show ? 0 : 1;
            float end = show ? 1 : 0;

            this.appearAnimation?.Cancel();
            this.appearAnimation = new FloatAnimationWorkAction(this.Owner, start, end, TimeSpan.FromSeconds(0.4f), EaseFunction.SineInOutEase, (f) =>
            {
                if (this.currentHand == XRHandedness.LeftHand)
                {
                    this.handMenuTransform.LocalRotation = Vector3.Lerp(new Vector3(0, MathHelper.PiOver2, 0), new Vector3(0, -MathHelper.Pi, 0), f);
                }
                else
                {
                    this.handMenuTransform.LocalRotation = Vector3.Lerp(new Vector3(0, -MathHelper.PiOver2, 0), new Vector3(0, -MathHelper.Pi, 0), f);
                }
            });
            this.appearAnimation.Run();
        }

        private void ExtendedAnimation(bool extended)
        {
            if (this.menuExtended == extended)
            {
                return;
            }

            this.menuExtended = extended;

            float start = extended ? 0 : 1;
            float end = extended ? 1 : 0;

            this.extendedAnimation?.Cancel();
            this.extendedAnimation = new ActionWorkAction(() =>
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
                    this.frontPlateTransform.Scale = Vector3.Lerp(new Vector3(this.numberOfColumns, this.numberButtonsPerColumns, 1), new Vector3(this.numberButtonsPerColumns, this.numberOfColumns, 1), f);
                    this.backPlateTransform.Scale = Vector3.Lerp(new Vector3(this.numberOfColumns, 1, 1), new Vector3(this.numberButtonsPerColumns, 1, 1), f);

                    // Header animation
                    this.detachButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(ButtonWidthOverTwo, 0, 0.003f), new Vector3(ButtonWidthOverTwo + (ButtonWidth * (this.numberButtonsPerColumns - 1)), 0, 0.003f), f);
                    this.followButtonTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidthOverTwo, 0, 0.003f), new Vector3(-ButtonWidthOverTwo + (ButtonWidth * (this.numberButtonsPerColumns - 1)), 0, 0.003f), f);

                    this.textTransform.LocalPosition = Vector3.Lerp(new Vector3(-ButtonWidth * 2, 0, 0.003f), new Vector3(0.015f, 0, 0.003f), f);
                    this.text3DMesh.Color = Color.Lerp(Color.Transparent, Color.White, f);

                    // Buttons animation
                    int i = 0;
                    foreach (var button in this.instantiatedButtons.Values.Reverse())
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
            this.extendedAnimation.Run();
        }

        private void DetachButtonToggle_Toggled(object sender, EventArgs e)
        {
            this.palmPanelBehavior.Owner.IsEnabled = !this.detachButtonToggle.IsOn;
            this.ExtendedAnimation(this.detachButtonToggle.IsOn);
        }

        // -- Begin Debug area --

        [BindService]
        protected GraphicsPresenter graphicsPresenter;
        private XRHandedness currentHand;

        protected override void Update(TimeSpan gameTime)
        {
            KeyboardDispatcher keyboardDispatcher = this.graphicsPresenter.FocusedDisplay?.KeyboardDispatcher;

            if (keyboardDispatcher?.ReadKeyState(Keys.K) == ButtonState.Pressing)
            {
                this.ExtendedAnimation(true);
            }
            else if (keyboardDispatcher?.ReadKeyState(Keys.J) == ButtonState.Pressing)
            {
                this.ExtendedAnimation(false);
            }

            if (keyboardDispatcher?.ReadKeyState(Keys.O) == ButtonState.Pressing)
            {
                this.AppearAnimation(true);
            }
            else if (keyboardDispatcher?.ReadKeyState(Keys.P) == ButtonState.Pressing)
            {
                this.AppearAnimation(false);
            }

            if (keyboardDispatcher?.ReadKeyState(Keys.I) == ButtonState.Pressing)
            {
                this.AddButton();
            }
        }

        private void AddButton()
        {
            this.ButtonDescriptions.Add(new HandMenuButtonDescription
            {
                IsToggle = false,
                TextOn = this.buttonDescriptions.Count.ToString(),
                IconOn = CoreResourcesIDs.Materials.Icons.Help,
            });
        }

        // -- End Debug area --
    }
}
