using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using System;
using Xrv.AudioNote.Messages;
using Xrv.AudioNote.Models;
using Xrv.Core;

namespace Xrv.AudioNote
{
    public enum AudioNoteAnchorVisual
    {
        Empty,
        Recorded,
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
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner, tag: "Grab")]
        protected MaterialComponent grabMaterial;

        [BindComponent]
        protected AudioNoteAnchorHandler handler;

        [BindComponent]
        protected TapDetector tapDetector;

        private AudioNoteData audioNote;

        public AudioNoteData AudioNote
        {
            get => audioNote;
            set
            {
                audioNote = value;
                if (this.IsAttached)
                {
                    if (!string.IsNullOrEmpty(audioNote.Path))
                    {
                        this.UpdateVisualState(AudioNoteAnchorVisual.Recorded);
                    }
                }
            }
        }

        public void UpdateVisualState(AudioNoteAnchorVisual current)
        {
            switch (current)
            {
                case AudioNoteAnchorVisual.Empty:
                    iconMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteIconEmpty);
                    backMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteBackEmpty);
                    break;
                default:
                    iconMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteIconFull);
                    backMaterial.Material = this.assetsService.Load<Material>(AudioNoteResourceIDs.Materials.AudioNoteBackFull);
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

            this.tapDetector.OnTap += Handler_OnClick;
            return true;
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            this.tapDetector.OnTap -= Handler_OnClick;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
        }

        private void Handler_OnClick(object sender, EventArgs e)
        {
            this.xrvService.PubSub.Publish(new AudioNoteMessage()
            {
                Data = this.AudioNote,
            });
        }
    }
}
