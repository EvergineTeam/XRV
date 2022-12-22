// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrv.Painter.Enums;
using Xrv.Painter.Helpers;
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
    public class PainterManager : Component
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

        private const string LINETAG = "linetag";

        private List<PainterAction> actions = new List<PainterAction>();

        private PencilMesh pencilMesh;
        private IEnumerable<PressableButton> modeButtons;
        private IEnumerable<PressableButton> thicknessButtons;
        private IEnumerable<PressableButton> commandsButtons;
        private PainterThickness thickness;
        private PainterModes mode;

        /// <summary>
        /// On mode changed.
        /// </summary>
        public event EventHandler<PainterModes> OnModeChanged;

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
                    this.OnModeChanged?.Invoke(this, value);
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
            }
        }

        /// <summary>
        /// Gets or sets color.
        /// </summary>
        public ColorEnum Color { get; set; }

        /// <summary>
        /// Gets or sets selected material.
        /// </summary>
        public Material SelectedMaterial { get; set; }

        /// <summary>
        /// Undo last action remove/add line.
        /// </summary>
        public void Undo()
        {
            if (this.actions.Any())
            {
                var index = this.actions.Count - 1;
                var last = this.actions[index];

                if (last.Mode == PainterModes.Painter)
                {
                    this.Owner.EntityManager.Remove(last.Entity);
                }
                else if (last.Mode == PainterModes.Eraser)
                {
                    var line = this.CreateEntity(this.Color);
                    this.Owner.EntityManager.Add(line.entity);
                    line.mesh.LinePoints = last.Line;
                    line.mesh.RefreshMeshes();

                    // Add line so it can be removed again with undo.
                    this.actions.Add(new PainterAction()
                    {
                        Mode = PainterModes.Painter,
                        Line = last.Line,
                        Entity = line.entity,
                    });
                }

                this.actions.RemoveAt(index);
            }
        }

        /// <summary>
        /// Removes all lines.
        /// </summary>
        public void ClearAll()
        {
            this.actions.Clear();
            var lines = this.Owner.EntityManager.FindAllByTag(LINETAG).ToList();
            foreach (var item in lines)
            {
                this.Owner.EntityManager.Remove(item);
            }
        }

        /// <summary>
        /// Do remove.
        /// </summary>
        /// <param name="position">Cursor position.</param>
        public void DoErase(Vector3 position)
        {
            var sphere = new BoundingSphere(position, 0.1f);
            var collision = this.FindCollision(sphere);
            if (collision != null)
            {
                this.actions.Add(new PainterAction()
                {
                    Mode = this.Mode,
                    Line = collision.FindComponent<PencilMesh>().LinePoints,
                });

                this.Owner.EntityManager.Remove(collision);
            }
        }

        /// <summary>
        /// Do paint.
        /// </summary>
        /// <param name="position">Cursor position.</param>
        public void DoPaint(Vector3 position)
        {
            var lInfo = new LineInfo()
            {
                Color = this.Color,
                Position = position,
                Thickness = this.GetThickNess(this.Thickness),
                PainterThickness = this.thickness,
            };

            if (this.pencilMesh == null)
            {
                // Creates first point
                var pencil = this.CreateEntity(this.Color);
                this.pencilMesh = pencil.mesh;
                var line = pencil.entity;

                this.Owner.EntityManager.Add(line);

                this.actions.Add(new PainterAction()
                {
                    Mode = this.Mode,
                    Line = this.pencilMesh.LinePoints,
                    Entity = line,
                });
            }

            this.pencilMesh.LinePoints.Add(lInfo);
            this.pencilMesh.RefreshMeshes();
        }

        /// <summary>
        /// End painting.
        /// </summary>
        public void EndPaint()
        {
            this.pencilMesh = null;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            this.modeButtons = this.modeEntity.FindComponentsInChildren<PressableButton>();
            this.thicknessButtons = this.thicknessEntity.FindComponentsInChildren<PressableButton>();
            this.commandsButtons = this.commandsEntity.FindComponentsInChildren<PressableButton>();

            if (Application.Current.IsEditor)
            {
                return true;
            }

            foreach (var button in this.modeButtons)
            {
                button.ButtonReleased += this.ModeButton_ButtonReleased;
            }

            foreach (var button in this.thicknessButtons)
            {
                button.ButtonReleased += this.ThicknessButtonsButton_ButtonReleased;
            }

            foreach (var button in this.commandsButtons)
            {
                button.ButtonReleased += this.CommandsButtonsButtonsButton_ButtonReleased;
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();

            if (Application.Current.IsEditor)
            {
                return;
            }

            foreach (var button in this.modeButtons)
            {
                button.ButtonReleased -= this.ModeButton_ButtonReleased;
            }

            foreach (var button in this.thicknessButtons)
            {
                button.ButtonReleased -= this.ThicknessButtonsButton_ButtonReleased;
            }

            foreach (var button in this.commandsButtons)
            {
                button.ButtonReleased -= this.CommandsButtonsButtonsButton_ButtonReleased;
            }
        }

        private Entity FindCollision(BoundingSphere bounding)
        {
            foreach (var line in this.Owner.EntityManager.FindAllByTag(LINETAG).ToList())
            {
                var mesh = line.FindComponent<PencilMesh>();
                if (mesh.CheckLineCollision(bounding))
                {
                    return line;
                }
            }

            return null;
        }

        private float GetThickNess(PainterThickness thickness)
        {
            switch (thickness)
            {
                case PainterThickness.Thin:
                    return 0.001f;
                case PainterThickness.Medium:
                    return 0.005f;
                default:
                    return 0.01f;
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

        private (Entity entity, PencilMesh mesh) CreateEntity(ColorEnum color)
        {
            var mesh = new PencilMesh(); // { IsDebugMode = true, };

            var entity = new Entity($"line_{Guid.NewGuid()}")
            {
                Tag = LINETAG,
            }
                .AddComponent(new Transform3D())
                .AddComponent(mesh)
                .AddComponent(new MaterialComponent()
                {
                    Material = this.assetsService.Load<Material>(ColorHelper.GetMaterialFromColor(color)),
                })
                .AddComponent(new MeshRenderer());

            return (entity, mesh);
        }
    }
}
