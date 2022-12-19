// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.Properties.Session
{
    internal class UpdateGlobalAction : IUpdateAction
    {
        private readonly string propertyName;
        private readonly object propertyValue;

        public UpdateGlobalAction(string propertyName, object propertyValue)
        {
            this.propertyName = propertyName;
            this.propertyValue = propertyValue;
        }

        public void Update(SessionData data)
        {
            switch (this.propertyName)
            {
                case nameof(SessionData.PresenterId):
                    data.PresenterId = (int)this.propertyValue;
                    break;
            }
        }
    }
}
