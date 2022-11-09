// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Components.Fonts;
using Evergine.Framework;
using System;
using System.Linq;
using Xrv.Core.Messaging;

namespace Xrv.Core.Networking.Settings
{
    /// <summary>
    /// Controls session while user is creating/joining a session.
    /// </summary>
    public class CreatingOrJoiningSessionController : Component
    {
        [BindService]
        private XrvService xrvService = null;

        private Text3DMesh statusText = null;
        private Guid subscription;
        private PubSub pubSub = null;

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.pubSub = this.xrvService.PubSub;
                this.statusText = this.Owner
                    .FindChildrenByTag("PART_Session_Settings_StatusText", true)
                    .First()
                    .FindComponentInChildren<Text3DMesh>();
                this.subscription = this.pubSub.Subscribe<SessionStatusChangeMessage>(this.OnSessionStatusChange);
                this.Owner.IsEnabled = Application.Current.IsEditor;
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

            if (this.statusText != null)
            {
                var session = this.xrvService.Networking.Session;
                this.statusText.Text = session.CurrentUserIsHost ? "Creating Session..." : "Joining Session...";
            }
        }

        private void OnSessionStatusChange(SessionStatusChangeMessage message) =>
            this.Owner.IsEnabled = message.NewStatus == SessionStatus.Joining;
    }
}
