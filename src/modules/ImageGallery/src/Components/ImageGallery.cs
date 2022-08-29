using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Xrv.ImageGallery.Components
{
    public class ImageGallery : Component
    {
        //[BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_next")]
        private PressableButton nextButton;
        //[BindComponent(source: BindComponentSource.Children, tag: "PART_image_gallery_previous")]
        private PressableButton previousButton;

        [BindEntity(source:BindEntitySource.Children, tag: "PART_image_gallery_next")]
        private Entity nextButtonEntity;
        
        [BindEntity(source:BindEntitySource.Children, tag: "PART_image_gallery_previous")]
        private Entity previousButtonEntity;

        protected override bool OnAttached()
        {
            this.nextButton = nextButtonEntity.FindComponentInChildren<PressableButton>();
            this.previousButton = previousButtonEntity.FindComponentInChildren<PressableButton>();

            this.nextButton.ButtonPressed += this.onNextButtonPressed;
            this.previousButton.ButtonPressed += this.onPreviousButtonPressed;
            return base.OnAttached();
        }

        protected override void OnDetach()
        {
            this.nextButton.ButtonPressed -= this.onNextButtonPressed;
            this.previousButton.ButtonPressed -= this.onPreviousButtonPressed;
            base.OnDetach();
        }

        public void onNextButtonPressed(object sender, EventArgs e)
        {
            Debug.WriteLine("NEXT");
        }

        public void onPreviousButtonPressed(object sender, EventArgs e)
        {
            Debug.WriteLine("PREVIOUS");
        }
    }
}
