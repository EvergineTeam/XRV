// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System.Globalization;

namespace Evergine.Xrv.Core.Localization
{
    /// <summary>
    /// Message sent when user changes application language.
    /// </summary>
    public class CurrentCultureChangeMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentCultureChangeMessage"/> class.
        /// </summary>
        /// <param name="culture">New culture.</param>
        public CurrentCultureChangeMessage(CultureInfo culture)
        {
            this.Culture = culture;
        }

        /// <summary>
        /// Gets current culture.
        /// </summary>
        public CultureInfo Culture { get; private set; }
    }
}
