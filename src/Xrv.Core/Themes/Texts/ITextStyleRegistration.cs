// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Collections.Generic;

namespace Xrv.Core.Themes.Texts
{
    /// <summary>
    /// Text style registration, to make style globally available.
    /// </summary>
    public interface ITextStyleRegistration
    {
        /// <summary>
        /// Extension point to register text styles.
        /// </summary>
        /// <param name="registrations">Registrations dictionary instance.</param>
        void Register(Dictionary<string, TextStyle> registrations);
    }
}
