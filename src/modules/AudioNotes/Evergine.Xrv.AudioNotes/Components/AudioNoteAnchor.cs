// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.MRTK.Effects;
using System;
using Evergine.Xrv.AudioNotes.Models;
using Evergine.Xrv.Core;

namespace Evergine.Xrv.AudioNotes
{
    /// <summary>
    /// Audio note anchor visual enum.
    /// </summary>
    public enum AudioNoteAnchorVisual
    {
        /// <summary>
        /// Idle state.
        /// </summary>
        Idle,

        /// <summary>
        /// Selected state.
        /// </summary>
        Selected,

        /// <summary>
        /// Grabbed state.
        /// </summary>
        Grabbed,
    }

    /// <summary>
    /// Audio note anchor.
    /// </summary>
    public class AudioNoteAnchor : Component
    {
        /// <summary>
        /// Xrv Service.
        /// </summary>
        [BindService]
        protected XrvService xrvService;

        /// <summary>
        /// Assets Service.
        /// </summary>
        [BindService]
        protected AssetsService assetsService = null;

        /// <summary>
        /// iconMaterial.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Icon")]
        protected MaterialComponent iconMaterial;

        /// <summary>
        /// backMaterial.
        /// </summary>
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Back")]
        protected MaterialComponent backMaterial;

        private AudioNoteData audioNote;

        /// <summary>
        /// Gets or sets a value indicating whether isSelected.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Gets or sets audioNote.
        /// </summary>
        public AudioNoteData AudioNote
        {
            get => this.audioNote;
            set
            {
                this.audioNote = value;
            }
        }

        /// <summary>
        /// Update Visual State.
        /// </summary>
        /// <param name="current">Visual state to set.</param>
        public void UpdateVisualState(AudioNoteAnchorVisual current)
        {
            switch (current)
            {
                case AudioNoteAnchorVisual.Grabbed:
                    this.iconMaterial.Material = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AnchorGrabbed);
                    break;
                case AudioNoteAnchorVisual.Selected:
                    this.iconMaterial.Material = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AnchorIdle);
                    this.backMaterial.Material = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AnchorSelected);
                    break;
                default:
                    this.iconMaterial.Material = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AnchorIdle);
                    this.backMaterial.Material = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AudioNoteAnchorBack);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            if (!base.OnAttached())
            {
                return false;
            }

            if (Application.Current.IsEditor)
            {
                return true;
            }

            this.AudioNote = new AudioNoteData()
            {
                Guid = Guid.NewGuid().ToString(),
            };

            ////BackgroundPrimaryColor "#041C2CFF"
            ////BackgroundSecondaryColor "#000000FF"
            ////ForegroundPrimaryColor "#115BB8FF"
            ////ForegroundSecondaryColor "#DF4661FF"

            // Set Anchor colo themes
            var anchorGrabbed = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AnchorSelected);
            var grabMat = new HoloGraphic(anchorGrabbed);
            grabMat.Albedo = new Color("#115BB8FF"); // TODO change wit theme colors

            var anchorback = this.assetsService.Load<Material>(AudioNotesResourceIDs.Materials.AudioNoteAnchorBack);
            var backMat = new HoloGraphic(anchorback);
            backMat.Albedo = new Color("#041C2CFF"); // TODO change wit theme colors

            return true;
        }
    }
}
