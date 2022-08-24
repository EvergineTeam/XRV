using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.MRTK.Effects;
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
        Open,
        Closed,
    }

    public class AudioNoteAnchor : Component
    {
        [BindService()]
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
        private AudioNoteAnchorVisual visualState;
        private AudioNoteAnchorState state;

        public AudioNoteData AdutioNote { get; set; }

        public AudioNoteAnchorState AnchorState
        {
            get => state; set
            {
                state = value;
                if (this.IsAttached)
                {
                    this.UpdateState(state);
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
            var primary = xrvService.CurrentTheme.BackgroundPrimaryColor;
            var secondary = Color.White;

            var icon = new HoloGraphic(iconMaterial.Material);
            var back = new HoloGraphic(backMaterial.Material);

            if (current != AudioNoteAnchorVisual.Empty)
            {
                primary = Color.White;
                secondary = xrvService.CurrentTheme.BackgroundPrimaryColor;
            }

            icon.Parameters_Color = secondary.ToVector3();
            back.Parameters_Color = primary.ToVector3();
        }

        public void UpdateState(AudioNoteAnchorState current)
        {
            this.xrvService.PubSub.Publish(new AudioNoteMessage()
            {
                Data = this.AdutioNote,
                State = current
            });
        }

        protected override bool OnAttached()
        {
            if (!base.OnAttached()) return false;
            if (Application.Current.IsEditor) return true;
            this.iconMaterial.Material = this.assetsService.Load<Material>(this.iconMaterial.Material.Id, forceNewInstance: true);
            this.backMaterial.Material = this.assetsService.Load<Material>(this.backMaterial.Material.Id, forceNewInstance: true);

            this.handler.OnClick += Handler_OnClick;
            return true;
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
