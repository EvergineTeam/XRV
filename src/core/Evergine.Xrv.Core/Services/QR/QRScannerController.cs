﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Framework;
using Evergine.Framework.XR.QR;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Services.QR
{
    /// <summary>
    /// Controls QR scanner user interface.
    /// </summary>
    public class QRScannerController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindService(isRequired: false)]
        private IQRCodeWatcherService watcher = null;

        private PressableButton closeButton;
        private CancellationTokenSource scanCts;
        private ILogger logger;

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
                this.logger = this.xrvService.Services.Logging;

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
            using (this.logger?.BeginScope("Starting QR scanning"))
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
                        this.logger?.LogWarning("Could not grant access to camera and QR scanner");
                    }
                }
                catch (Exception ex)
                {
                    this.logger?.LogError(ex, "Error scanning QR code");
                }
            }
        }

        private void CloseButton_ButtonReleased(object sender, EventArgs e)
        {
            var scanningFlow = this.xrvService.Services.QrScanningFlow;
            scanningFlow.Stop();
        }

        private async Task<bool> EnsurePermissionsGrantedAsync()
        {
            return await Task.FromResult(true);
        }
    }
}
