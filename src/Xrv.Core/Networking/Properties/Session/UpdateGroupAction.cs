// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.Session
{
    internal class UpdateGroupAction : IUpdateAction
    {
        private readonly SessionDataGroup group;

        public UpdateGroupAction(SessionDataGroup group)
        {
            this.group = group;
        }

        public void Update(SessionData data)
        {
            data.SetGroupData(this.group);
        }
    }
}
