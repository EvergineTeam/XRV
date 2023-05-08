// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;
using System.Linq;
using Evergine.Components.Fonts;
using Evergine.Framework;
using Evergine.MRTK.SDK.Features.UX.Components.PressableButtons;
using Evergine.Networking.Client;
using Evergine.Xrv.Core.UI.Dialogs;
using Microsoft.Extensions.Logging;
using WindowsSystem = Evergine.Xrv.Core.UI.Windows.WindowsSystem;

namespace Evergine.Xrv.Core.Networking.Settings
{
    /// <summary>
    /// Controls leave session user interface.
    /// </summary>
    public class LeaveSessionController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        [BindService]
        private MatchmakingClientService client = null;

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
                this.windows = this.xrvService.WindowsSystem;
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
                this.SetConnectionInformationText();
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

        private void SetConnectionInformationText()
        {
            var session = this.xrvService.Networking.Session;
            var formatMessage = session.CurrentUserIsHost
                ? this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Joined_StatusTextAsServer)
                : this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_Joined_StatusTextAsClient);

            this.joinedStateText.Text = string.Format(formatMessage, session.Host.Name, this.client.LocalPlayer.Nickname);
        }

        private void EndSessionButton_ButtonReleased(object sender, EventArgs e)
        {
            ConfirmationDialog dialog;
            if (this.networking.Session.CurrentUserIsHost)
            {
                dialog = this.windows.ShowConfirmationDialog(
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_End_EndSessionConfirmationTitle),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_End_EndSessionConfirmationMessage),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Cancel),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Accept));
            }
            else
            {
                dialog = this.windows.ShowConfirmationDialog(
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_End_LeaveSessionConfirmationTitle),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_End_LeaveSessionConfirmationMessage),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Cancel),
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Accept));
            }

            dialog.Closed += this.Dialog_Closed;
        }

        private async void Dialog_Closed(object sender, EventArgs e)
        {
            var dialog = sender as ConfirmationDialog;
            if (dialog == null)
            {
                return;
            }

            dialog.Closed -= this.Dialog_Closed;
            if (dialog.Result != ConfirmationDialog.AcceptKey)
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
                this.windows.ShowAlertDialog(
                    this.xrvService.Localization.GetString(() => Resources.Strings.Settings_Sessions_End_Error),
                    ex.Message,
                    this.xrvService.Localization.GetString(() => Resources.Strings.Global_Ok));
            }
        }
    }
}
