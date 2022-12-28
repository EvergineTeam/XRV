﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Xrv.Core.UI.Dialogs;
using WindowsSystem = Xrv.Core.UI.Windows.WindowsSystem;

namespace Xrv.Core.Networking.Settings
{
    /// <summary>
    /// Controls leave session user interface.
    /// </summary>
    public class LeaveSessionController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private ILogger logger;
        private Text3DMesh joinedStateText = null;
        private PressableButton endSessionButton = null;
        private NetworkSystem networking = null;
        private WindowsSystem windows = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.joinedStateText = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_JoinedSession_Text", true)
                    .First()
                    .FindComponentInChildren<Text3DMesh>();
                this.endSessionButton = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_EndSession_Button", true)
                    .First()
                    .FindComponentInChildren<PressableButton>();

                this.Owner.IsEnabled = Application.Current.IsEditor;
                this.logger = this.xrvService.Services.Logging;
                this.networking = this.xrvService.Networking;
                this.windows = this.xrvService.WindowSystem;
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (!Application.Current.IsEditor)
            {
                this.endSessionButton.ButtonReleased += this.EndSessionButton_ButtonReleased;
            }

            if (this.joinedStateText != null)
            {
                var session = this.xrvService.Networking.Session;
                this.joinedStateText.Text = session.CurrentUserIsHost
                    ? $"You have created and joined {session.Host.Name}"
                    : $"You have joined {session.Host.Name}";
            }
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            base.OnDeactivated();

            if (!Application.Current.IsEditor)
            {
                this.endSessionButton.ButtonReleased -= this.EndSessionButton_ButtonReleased;
            }
        }

        private void EndSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            ConfirmDialog dialog;
            if (this.networking.Session.CurrentUserIsHost)
            {
                dialog = this.windows.ShowConfirmDialog(
                    "End session?",
                    "If you end the session, it will finish for anyone connected",
                    "Cancel",
                    "Accept");
            }
            else
            {
                dialog = this.windows.ShowConfirmDialog(
                    "Leave session?",
                    "You can join again to the session, if it is still available",
                    "Cancel",
                    "Accept");
            }

            dialog.Closed += this.Dialog_Closed;
        }

        private async void Dialog_Closed(object sender, EventArgs e)
        {
            var dialog = sender as ConfirmDialog;
            if (dialog == null)
            {
                return;
            }

            dialog.Closed -= this.Dialog_Closed;
            if (dialog.Result != ConfirmDialog.AcceptKey)
            {
                return;
            }

            try
            {
                await this.networking.LeaveSessionAsync();
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error leaving session");
                this.windows.ShowAlertDialog("Error leaving session", ex.Message, "OK");
            }
        }
    }
}
