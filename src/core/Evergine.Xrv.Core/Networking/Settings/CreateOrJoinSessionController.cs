// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Evergine.Xrv.Core.Networking.Settings
{
    /// <summary>
    /// Controls session creation/join user interface.
    /// </summary>
    public class CreateOrJoinSessionController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindComponent]
        private SessionScanner sessionScanner = null;

        private NetworkSystem networkSystem;
        private Text3DMesh sessionNameText = null;
        private Text3DMesh selectedSessionText = null;
        private PressableButton createSessionButton = null;
        private PressableButton joinToSessionButton = null;
        private SessionHostInfo selectedHost = null;

        private ILogger logger = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.networkSystem = this.xrvService.Networking;
                this.sessionNameText = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_SessionName_Text", true)
                    .First()
                    .FindComponentInChildren<Text3DMesh>();
                this.selectedSessionText = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_JoinSession_Text", true)
                    .First()
                    .FindComponentInChildren<Text3DMesh>();
                this.createSessionButton = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_CreateSession_Button", true)
                    .First()
                    .FindComponentInChildren<PressableButton>();
                this.joinToSessionButton = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_Connect_Button", true)
                    .First()
                    .FindComponentInChildren<PressableButton>();
                this.sessionNameText.Text = this.CreateRandomSessionName();
                this.logger = this.xrvService.Services.Logging;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (!Application.Current.IsEditor)
            {
                this.createSessionButton.ButtonReleased += this.CreateSessionButton_ButtonReleased;
                this.joinToSessionButton.ButtonReleased += this.ConnectToSessionButton_ButtonReleased;
            }

            this.sessionScanner.ScanningResultsUpdated += this.SessionScanner_ScanningResultsUpdated;
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (!Application.Current.IsEditor)
            {
                this.createSessionButton.ButtonReleased -= this.CreateSessionButton_ButtonReleased;
                this.joinToSessionButton.ButtonReleased -= this.ConnectToSessionButton_ButtonReleased;
            }

            this.sessionScanner.ScanningResultsUpdated -= this.SessionScanner_ScanningResultsUpdated;
        }

        private string CreateRandomSessionName() =>
            string.Format(
                this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Create_SessionName),
                Guid.NewGuid().ToString().Substring(0, 6).ToUpper());

        private async void CreateSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            this.sessionScanner.StopScanning();

            ConnectionResult result = null;
            Exception exception = null;

            try
            {
                result = await this.networkSystem
                    .StartSessionAsync(this.sessionNameText.Text)
                    .ConfigureAwait(false);
                if (result.Succeeded)
                {
                    this.sessionScanner.StartScanning();
                }
            }
            catch (Exception ex)
            {
                result = new ConnectionResult
                {
                    Succeeded = false,
                    CancelledByUser = true,
                };
                exception = ex;
                this.logger?.LogError(ex, "Error creating session");
            }

            if (!result.Succeeded)
            {
                await this.ClearSessionOnErrorAsync().ConfigureAwait(false);

                if (!result.CancelledByUser)
                {
                    this.xrvService.WindowsSystem.ShowAlertDialog(
                        this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Create_Error),
                        exception?.Message ?? string.Empty,
                        this.xrvService.Localization.GetString(() => Resources.Strings.Global_Ok));
                }
            }
        }

        private async void ConnectToSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            var serverHost = this.selectedHost;
            if (serverHost == null)
            {
                return;
            }

            ConnectionResult result = null;
            Exception exception = null;

            try
            {
                result = await this.networkSystem.ConnectToSessionAsync(serverHost).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result = new ConnectionResult
                {
                    Succeeded = false,
                    CancelledByUser = true,
                };
                exception = ex;
                this.logger?.LogError(ex, "Error connecting session");
            }

            if (!result.Succeeded)
            {
                await this.ClearSessionOnErrorAsync().ConfigureAwait(false);

                if (!result.CancelledByUser)
                {
                    this.xrvService.WindowsSystem.ShowAlertDialog(
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Join_Error),
                    exception?.Message ?? string.Empty,
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Ok));
                }
            }
        }

        private void SessionScanner_ScanningResultsUpdated(object sender, EventArgs e)
        {
            this.selectedHost = this.sessionScanner.AvailableSessions.FirstOrDefault();
            this.selectedSessionText.Text = this.selectedHost?.Name
                ?? this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Join_NoSessionsFound);
        }

        private Task ClearSessionOnErrorAsync()
        {
            var session = this.networkSystem.Session;
            session.ActivelyClosedByClient = true;
            return this.networkSystem.ClearSessionStatusAsync();
        }
    }
}
