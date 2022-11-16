// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.XR.QR;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xrv.Core.Services.QR
{
    /// <summary>
    /// Controls QR scanner user interface.
    /// </summary>
    public class QrScannerController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindService(isRequired: false)]
        private IQRCodeWatcherService watcher = null;

        private PressableButton closeButton;
        private CancellationTokenSource scanCts;

        /// <summary>
        /// Cancels current scanning flow.
        /// </summary>
        public void Cancel()
        {
            this.scanCts?.Cancel();
            this.scanCts = null;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                var buttonContainer = this.Owner.FindChildrenByTag("PART_qrscanner_close", isRecursive: true).First();
                this.closeButton = buttonContainer.FindComponentInChildren<PressableButton>();
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.watcher == null)
            {
                return;
            }

            if (!this.watcher.IsWatcherRunning)
            {
                this.Cancel();
                this.scanCts = new CancellationTokenSource();
                _ = this.StartScanningAsync(this.scanCts.Token);
            }

            this.closeButton.ButtonReleased += this.CloseButton_ButtonReleased;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (this.watcher == null)
            {
                return;
            }

            this.Cancel();
            this.closeButton.ButtonReleased -= this.CloseButton_ButtonReleased;
        }

        private async Task StartScanningAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                bool granted = await this.EnsurePermissionsGrantedAsync();
                if (granted)
                {
                    await this.watcher.StartQRWatchingAsync(cancellationToken).ConfigureAwait(false);
                    this.watcher.ClearQRCodes();
                }
                else
                {
                    Debug.WriteLine("Could not grant access to camera and QR scanner");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void CloseButton_ButtonReleased(object sender, EventArgs e)
        {
            var scanningFlow = this.xrvService.Services.QrScanningFlow;
            scanningFlow.Stop();
        }

        private async Task<bool> EnsurePermissionsGrantedAsync()
        {
#if UWP
            bool granted = true;

            if (Microsoft.MixedReality.QR.QRCodeWatcher.IsSupported()
                && (!await Utils.DeviceHelper.EnsureCameraPersmissionAsync() ||
                   await Microsoft.MixedReality.QR.QRCodeWatcher.RequestAccessAsync() != Microsoft.MixedReality.QR.QRCodeWatcherAccessStatus.Allowed))
            {
                granted = false;
            }

            return granted;
#else
            return await Task.FromResult(true);
#endif
        }
    }
}
