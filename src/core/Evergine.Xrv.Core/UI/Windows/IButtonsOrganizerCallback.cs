// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.UI.Windows
{
    /// <summary>
    /// Callback to be invoked on button reorganization.
    /// </summary>
    internal interface IButtonsOrganizerCallback
    {
        /// <summary>
        /// Invoked before buttons layout is updated.
        /// </summary>
        void BeforeUpdatingLayout();

        /// <summary>
        /// Invoked after buttons layout is updated.
        /// </summary>
        void AfterUpdatingLayout();
    }
}
