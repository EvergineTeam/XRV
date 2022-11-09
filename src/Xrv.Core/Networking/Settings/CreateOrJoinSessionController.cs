// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using System;
using System.Diagnostics;
using System.Linq;
using Xrv.Core.Messaging;

namespace Xrv.Core.Networking.Settings
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

        private Guid subscription;
        private PubSub pubSub = null;
        private NetworkSystem networkSystem;
        private Text3DMesh sessionNameText = null;
        private Text3DMesh selectedSessionText = null;
        private PressableButton createSessionButton = null;
        private PressableButton joinToSessionButton = null;
        private SessionHostInfo selectedHost = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.networkSystem = this.xrvService.Networking;
                this.pubSub = this.xrvService.PubSub;

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
                this.subscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub?.Unsubscribe(this.subscription);
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
            $"Session #{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

        private async void CreateSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            var succeeded = false;
            this.sessionScanner.StopScanning();

            try
            {
                succeeded = await this.networkSystem.StartSessionAsync(this.sessionNameText.Text);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                this.xrvService.WindowSystem.ShowAlertDialog(
                    "Could not start session",
                    ex.Message,
                    "OK");
            }

            if (!succeeded)
            {
                this.sessionScanner.StartScanning();
                Trace.TraceError("Server could not be started");
                this.xrvService.WindowSystem.ShowAlertDialog(
                    "Could not start server",
                    string.Empty,
                    "OK");
            }
        }

        private async void ConnectToSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            var serverHost = this.selectedHost;
            if (serverHost == null)
            {
                return;
            }

            var succeeded = false;

            try
            {
                succeeded = await this.networkSystem.ConnectToSessionAsync(serverHost).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                this.xrvService.WindowSystem.ShowAlertDialog(
                    "Could not join session",
                    ex.Message,
                    "OK");
            }

            if (!succeeded)
            {
                Trace.TraceError("Could not join session");
                this.xrvService.WindowSystem.ShowAlertDialog(
                    "Could not join session",
                    string.Empty,
                    "OK");
            }
        }

        private void SessionScanner_ScanningResultsUpdated(object sender, EventArgs e)
        {
            this.selectedHost = this.sessionScanner.AvailableSessions.FirstOrDefault();
            this.selectedSessionText.Text = this.selectedHost?.Name ?? "No session found";
        }

        private void OnSessionStatusChange(SessionStatusChangeMessage message) =>
            this.Owner.IsEnabled = message.NewStatus == SessionStatus.Disconnected;
    }
}
