// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Xrv.Core.Networking.ControlRequest
{
    internal class SessionPresenterUpdatedMessage
    {
        public SessionPresenterUpdatedMessage(bool currentIsPresenter, int newPresenterId)
        {
            this.CurrentIsPresenter = currentIsPresenter;
            this.NewPresenterId = newPresenterId;
        }

        public bool CurrentIsPresenter { get; private set; }

        public int NewPresenterId { get; internal set; }
    }
}
