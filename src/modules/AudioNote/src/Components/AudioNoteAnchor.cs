using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.MRTK.Effects;
using System;
using Xrv.AudioNote.Models;
using Xrv.Core;

namespace Xrv.AudioNote
{
    public enum AudioNoteAnchorVisual
    {
        Idle,
        Selected,
        Grabbed
    }

    public class AudioNoteAnchor : Component
    {
        [BindService]
        protected XrvService xrvService;

        [BindService]
        protected AssetsService assetsService = null;

        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Icon")]
        protected MaterialComponent iconMaterial;
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Back")]
        protected MaterialComponent backMaterial;

        private AudioNoteData audioNote;

        public bool IsSelected { get; set; }

        public AudioNoteData AudioNote
        {
            get => audioNote;
            set
            {
                audioNote = value;
            }
        }

        public void UpdateVisualState(AudioNoteAnchorVisual current)
        {
            switch (current)
            {
                case AudioNoteAnchorVisual.Grabbed:
                    iconMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AnchorGrabbed);
                    ////backMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteAnchorBack);
                    break;
                case AudioNoteAnchorVisual.Selected:
                    iconMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AnchorIdle);
                    backMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AnchorSelected);
                    break;
                default:
                    iconMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AnchorIdle);
                    backMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteAnchorBack);
                    break;
            }
        }

        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;

            this.AudioNote = new AudioNoteData()
            {
                Guid = Guid.NewGuid().ToString(),
            };

            //BackgroundPrimaryColor "#041C2CFF"
            //BackgroundSecondaryColor "#000000FF"
            //ForegroundPrimaryColor "#115BB8FF"
            //ForegroundSecondaryColor "#DF4661FF"

            // Set Anchor colo themes
            var anchorGrabbed = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AnchorSelected);
            var grabMat = new HoloGraphic(anchorGrabbed);
            //grabMat.Albedo = this.xrvService.CurrentTheme.ForegroundPrimaryColor; // TODO change wit theme colors
            grabMat.Albedo = new Color("#115BB8FF");

            var anchorback = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteAnchorBack);
            var backMat = new HoloGraphic(anchorback);
            //grabMat.Albedo = this.xrvService.CurrentTheme.BackgroundPrimaryColor; // TODO change wit theme colors
            backMat.Albedo = new Color("#041C2CFF");

            return true;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
        }
    }
}
