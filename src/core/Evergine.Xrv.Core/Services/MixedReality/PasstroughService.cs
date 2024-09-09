// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common;
using Evergine.Components.XR;
using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Xrv.Core.Utils;

namespace Evergine.Xrv.Core.Services.MixedReality
{
    /// <summary>
    /// Service to control passthrough state. Note that this technology may only apply,
    /// right now, to some Android-based devices only.
    /// </summary>
    public class PasstroughService
    {
        private readonly EntityManager entityManager;
        private bool enablePassthrough;
        private XRPassthroughLayerComponent passthroughLayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasstroughService"/> class.
        /// </summary>
        /// <param name="entityManager">Entity manager.</param>
        public PasstroughService(EntityManager entityManager)
        {
            this.entityManager = entityManager;
        }

        /// <summary>
        /// Gets or sets a value indicating whether passthrough is enabled.
        /// </summary>
        public bool EnablePassthrough
        {
            get => this.enablePassthrough;

            set
            {
                if (this.enablePassthrough != value)
                {
                    this.enablePassthrough = value;
                    this.OnEnablePassthroughUpdated();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether passthrough is supported in current platform.
        /// </summary>
        public bool IsSupported => this.GetIsSupported();

        /// <summary>
        /// Gets Evergine component in charge of controlling passthrough state.
        /// </summary>
        public XRPassthroughLayerComponent PassthroughComponent => this.passthroughLayer;

        /// <summary>
        /// Turns on passthrough execution.
        /// </summary>
        public void Enable() => this.passthroughLayer.IsEnabled = true;

        /// <summary>
        /// Turns off passthrough execution.
        /// </summary>
        public void Disable() => this.passthroughLayer.IsEnabled = false;

        internal void Load()
        {
            this.passthroughLayer = new XRPassthroughLayerComponent();
            var passtroughEntity = new Entity("Passthrough")
                .AddComponent(this.passthroughLayer);

            this.OnEnablePassthroughUpdated();

            this.entityManager.Add(passtroughEntity);
        }

        private void OnEnablePassthroughUpdated()
        {
            if (this.passthroughLayer != null)
            {
                this.passthroughLayer.IsEnabled = this.enablePassthrough;
            }
        }

        private bool GetIsSupported()
        {
            // Currently, this only has sense in Android-based devices. HoloLens always
            // works with a "passthrough", so it's not an option in that platform.
            bool isXR = Application.Current.Container.Resolve<XRPlatform>() != null;
            return isXR && DeviceHelper.PlatformType == PlatformType.Android;
        }
    }
}
