using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Prefabs;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.Base.EventDatum.Input;
using Evergine.MRTK.Base.Interfaces.InputSystem.Handlers;
using Evergine.MRTK.Effects;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Painter.Models;

namespace Xrv.Painter.Components
{
    /// <summary>
    /// Painter thickness enum.
    /// </summary>
    public enum PainterThickness
    {
        /// <summary>
        /// Thin state.
        /// </summary>
        Thin,

        /// <summary>
        /// Medium state.
        /// </summary>
        Medium,

        /// <summary>
        /// Thick state.
        /// </summary>
        Thick,
    }

    /// <summary>
    /// Painter modes enum.
    /// </summary>
    public enum PainterModes
    {
        /// <summary>
        /// Initial state, no state,
        /// </summary>
        None,

        /// <summary>
        /// Hand state.
        /// </summary>
        Hand,

        /// <summary>
        /// Paint state.
        /// </summary>
        Painter,

        /// <summary>
        /// Remove state.
        /// </summary>
        Eraser,
    }

    /// <summary>
    /// Painter Manager add lines, and remove lines.
    /// </summary>
    public class PainterManager : Behavior, IMixedRealityPointerHandler
    {
        /// <summary>
        /// Assets service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService;

        /// <summary>
        /// Mode entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Mode")]
        protected Entity modeEntity;

        /// <summary>
        /// Thickness entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Thickness")]
        protected Entity thicknessEntity;

        /// <summary>
        /// Commands entity.
        /// </summary>
        [BindEntity(source: BindEntitySource.Children, tag: "Commands")]
        protected Entity commandsEntity;

        private List<Entity> lines = new List<Entity>();
        private List<PainterAction> actions = new List<PainterAction>();

        private bool isPointerDown;
        private Vector3 lastPosition;
        private bool firstTime;
        private Vector3 currentPosition;
        private TimeSpan current = TimeSpan.Zero;
        private TimeSpan betweenUpdate = TimeSpan.Zero;
        private float secondsBetweenUpdate = 1f;

        private LineMesh lineMesh;
        private List<LinePointInfo> linePoint;
        private Entity pointerEntity;
        private HoloGraphic pointerMaterial;
        private Transform3D pointerTransform;
        private IEnumerable<PressableButton> modeButtons;
        private IEnumerable<PressableButton> thicknessButtons;
        private IEnumerable<PressableButton> commandsButtons;
        private PainterThickness thickness;
        private PainterModes mode;

        /// <summary>
        /// Gets or sets mode.
        /// </summary>
        public PainterModes Mode
        {
            get => this.mode;
            set
            {
                this.mode = value;
                if (this.IsAttached)
                {
                    this.SetVisualMode(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets thickness.
        /// </summary>
        public PainterThickness Thickness
        {
            get => this.thickness;
            set
            {
                this.thickness = value;
                if (this.IsAttached)
                {
                    this.SetVisualThickness(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets pointer.
        /// </summary>
        public Entity Pointer { get; set; }

        /// <summary>
        /// Gets or sets color.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets selected material.
        /// </summary>
        public Material SelectedMaterial { get; set; }

        /// <summary>
        /// Gets or sets tiks.
        /// </summary>
        public float SecondsBetweenUpdate
        {
            get => this.secondsBetweenUpdate;
            set
            {
                this.secondsBetweenUpdate = value;
                this.betweenUpdate = TimeSpan.FromSeconds(this.secondsBetweenUpdate);
            }
        }

        /// <summary>
        /// Undo last action remove/add line.
        /// </summary>
        public void Undo()
        {

        }

        /// <summary>
        /// Removes all lines.
        /// </summary>
        public void ClearAll()
        {
            this.actions.Clear();
            foreach (var item in this.lines)
            {
                this.Owner.Scene.Managers.EntityManager.Remove(item);
            }

            this.lines.Clear();
        }

        /// <inheritdoc/>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            this.current = TimeSpan.Zero;
            this.isPointerDown = true;
            this.currentPosition = eventData.Position;
            this.lastPosition = eventData.Position;
            this.firstTime = true;

            this.pointerEntity.IsEnabled = this.Mode != PainterModes.Hand;
            if (this.pointerEntity.IsEnabled)
            {
                this.pointerMaterial.Albedo = this.Mode == PainterModes.Painter ? Color.White : Color.Red;
            }
        }

        /// <inheritdoc/>
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            this.currentPosition = eventData.Position;
            this.pointerTransform.Position = this.currentPosition;
        }

        /// <inheritdoc/>
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            this.isPointerDown = false;
            this.pointerEntity.IsEnabled = this.isPointerDown;
        }

        /// <inheritdoc/>
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.modeButtons = this.modeEntity.FindComponentsInChildren<PressableButton>();
            foreach (var button in this.modeButtons)
            {
                button.ButtonReleased += this.ModeButton_ButtonReleased;
            }

            this.thicknessButtons = this.thicknessEntity.FindComponentsInChildren<PressableButton>();
            foreach (var button in this.thicknessButtons)
            {
                button.ButtonReleased += this.ThicknessButtonsButton_ButtonReleased;
            }

            this.commandsButtons = this.commandsEntity.FindComponentsInChildren<PressableButton>();
            foreach (var button in this.commandsButtons)
            {
                button.ButtonReleased += this.CommandsButtonsButtonsButton_ButtonReleased;
            }

            if (Application.Current.IsEditor)
            {
                return true;
            }

            this.pointerEntity = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.PointerPainter).Instantiate();
            this.pointerMaterial = new HoloGraphic(this.pointerEntity.FindComponent<MaterialComponent>().Material);
            this.pointerTransform = this.pointerEntity.FindComponent<Transform3D>();
            this.Owner.EntityManager.Add(this.pointerEntity);

            return true;
        }

        /// <inheritdoc/>
        protected override void Update(TimeSpan gameTime)
        {
            if (this.Mode == PainterModes.Eraser)
            {
                this.DoRemove(gameTime);
            }
            else if (this.Mode == PainterModes.Painter)
            {
                this.DoPaint(gameTime);
            }
            else
            {
                return;
            }
        }

        private void DoRemove(TimeSpan gameTime)
        {
            if (!this.isPointerDown)
            {
                return;
            }

            this.current += gameTime;
            if (this.firstTime || this.current > this.betweenUpdate)
            {
                this.firstTime = false;
                var collision = this.FindCollision(0.1f);
                if (collision != null)
                {
                    this.actions.Add(new PainterAction()
                    {
                        Mode = this.Mode,
                        Line = collision.FindComponent<LineMesh>().LinePoints,
                    });

                    this.lines.Remove(collision);
                    this.Owner.EntityManager.Remove(collision);
                }
            }
        }

        private Entity FindCollision(float distance)
        {
            foreach (var line in this.lines)
            {
                var mesh = line.FindComponent<LineMesh>();
                foreach (var point in mesh.LinePoints)
                {
                    if (Vector3.Distance(this.currentPosition, point.Position) < distance)
                    {
                        return line;
                    }
                }
            }

            return null;
        }

        private void DoPaint(TimeSpan gameTime)
        {
            if (!this.isPointerDown)
            {
                return;
            }

            this.current += gameTime;
            if (this.firstTime)
            {
                // Creates first point
                this.firstTime = false;
                this.current = TimeSpan.Zero;
                var line = this.assetsService.Load<Prefab>(PainterResourceIDs.Prefabs.LinePainter).Instantiate();
                this.lines.Add(line);
                this.Owner.Scene.Managers.EntityManager.Add(line);

                this.lineMesh = line.FindComponent<LineMesh>();
                this.linePoint = new List<LinePointInfo>()
                    {
                        new LinePointInfo()
                        {
                            Color = this.Color,
                            Position = this.currentPosition,
                            Thickness = this.GetThickNess(this.Thickness),
                        },
                    };

                this.lineMesh.LinePoints = this.linePoint;
                this.actions.Add(new PainterAction()
                {
                    Mode = this.Mode,
                    Line = this.linePoint,
                });
            }
            else
            {
                if (this.current > this.betweenUpdate)
                {
                    this.current = TimeSpan.Zero;
                    if (Vector3.Distance(this.currentPosition, this.lastPosition) > 0.01f)
                    {
                        this.lastPosition = this.currentPosition;
                        this.linePoint.Add(new LinePointInfo()
                        {
                            Color = this.Color,
                            Position = this.currentPosition,
                            Thickness = this.GetThickNess(this.Thickness),
                        });
                        this.lineMesh.LinePoints = this.linePoint;
                    }
                }
            }
        }

        private float GetThickNess(PainterThickness thickness)
        {
            switch (thickness)
            {
                case PainterThickness.Thin:
                    return 0.01f;
                case PainterThickness.Medium:
                    return 0.04f;
                default:
                    return 0.08f;
            }
        }

        private void ModeButton_ButtonReleased(object sender, EventArgs e)
        {
            if (sender is PressableButton pressable
                && pressable.Owner.Parent is Entity button)
            {
                if (Enum.TryParse<PainterModes>(button.Name, out var mode))
                {
                    this.Mode = mode;
                }
                else
                {
                    throw new InvalidCastException($"{button.Name} not a valid PainterModes");
                }
            }
        }

        private void ThicknessButtonsButton_ButtonReleased(object sender, EventArgs e)
        {
            if (sender is PressableButton pressable
                && pressable.Owner.Parent is Entity button)
            {
                if (Enum.TryParse<PainterThickness>(button.Name, out var thickness))
                {
                    this.Thickness = thickness;
                }
                else
                {
                    throw new InvalidCastException($"{button.Name} not a valid PainterThickness");
                }
            }
        }

        private void CommandsButtonsButtonsButton_ButtonReleased(object sender, EventArgs e)
        {
            if (sender is PressableButton pressable
                && pressable.Owner.Parent is Entity button)
            {
                var name = button.Name;
                if (name == "Undo")
                {
                    this.Undo();
                }
                else if (name == "Clear")
                {
                    this.ClearAll();
                }
            }
        }

        private void SetVisualMode(PainterModes value)
        {
            foreach (var item in this.modeButtons)
            {
                var backPlate = item.Owner.FindChildrenByTag("PART_Plate", skipOwner: true).FirstOrDefault().FindComponent<MaterialComponent>();
                var name = item.Owner.Parent.Name;

                if (name == value.ToString())
                {
                    backPlate.Material = this.SelectedMaterial;
                }
                else
                {
                    backPlate.Material = null;
                }
            }
        }

        private void SetVisualThickness(PainterThickness value)
        {
            foreach (var item in this.thicknessButtons)
            {
                var backPlate = item.Owner.FindChildrenByTag("PART_Plate", skipOwner: true).FirstOrDefault().FindComponent<MaterialComponent>();
                var name = item.Owner.Parent.Name;

                if (name == value.ToString())
                {
                    backPlate.Material = this.SelectedMaterial;
                }
                else
                {
                    backPlate.Material = null;
                }
            }
        }
    }
}
