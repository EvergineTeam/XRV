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
    public enum AudioNoteAnchorState
    {
        Closed,
        Open,
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

        private AudioNoteAnchorVisual visualState;
        private AudioNoteAnchorState anchorState;

        public AudioNoteData AudioNote { get; set; }

        public AudioNoteAnchorState AnchorState
        {
            get => anchorState; set
            {
                anchorState = value;
                if (this.IsAttached)
                {
                    this.UpdateState(anchorState);
                }
            }
        }

        public AudioNoteAnchorVisual VisualState
        {
            get => visualState; set
            {
                visualState = value;
                if (this.IsAttached)
                {
                    this.UpdateVisualState(visualState);
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

        public void UpdateState(AudioNoteAnchorState current)
        {
            this.xrvService.PubSub.Publish(new AudioNoteMessage()
            {
                Data = this.AudioNote,
                State = current
            });
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
            var state = AudioNoteAnchorState.Open;
            if (this.AnchorState == AudioNoteAnchorState.Open)
            {
                state = AudioNoteAnchorState.Closed;
            }

            this.AnchorState = state;
        }
    }
}
