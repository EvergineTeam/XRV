// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Components.Fonts;
using Evergine.Framework;

namespace Evergine.Xrv.Core.Networking.Participants
{
    /// <summary>
    /// Displays participant name for a given avatar.
    /// </summary>
    public class AvatarDisplayName : Component
    {
        [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
        private Text3DMesh text3d = null;

        private string nickName;

        /// <summary>
        /// Gets or sets presented nickname.
        /// </summary>
        [IgnoreEvergine]
        [DontRenderProperty]
        public string Nickname
        {
            get => this.nickName;

            set
            {
                if (this.nickName != value)
                {
                    this.nickName = value;

                    if (this.IsAttached)
                    {
                        this.OnNickNameUpdate();
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached && !Application.Current.IsEditor)
            {
                this.OnNickNameUpdate();
            }

            return attached;
        }

        private void OnNickNameUpdate() => this.text3d.Text = this.Nickname;
    }
}
