// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Xrv.Core.Menu
{
    /// <summary>
    /// Button description.
    /// </summary>
    public class MenuButtonDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MenuButtonDescription"/> class.
        /// </summary>
        public MenuButtonDescription()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets description identifier.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether button should be created as a toggle button.
        /// </summary>
        public bool IsToggle { get; set; }

        /// <summary>
        /// Gets or sets material identifier for icon for standard button; or for On state in
        /// toggle buttons.
        /// </summary>
        public Guid IconOn { get; set; }

        /// <summary>
        /// Gets or sets material identifier for icon for Off state in
        /// toggle buttons. Has no effect in standard buttons.
        /// </summary>
        public Guid IconOff { get; set; }

        /// <summary>
        /// Gets or sets text for standard button; or for On state in
        /// toggle buttons.
        /// </summary>
        public string TextOn { get; set; }

        /// <summary>
        /// Gets or sets text for icon for Off state in
        /// toggle buttons. Has no effect in standard buttons.
        /// </summary>
        public string TextOff { get; set; }
    }
}
