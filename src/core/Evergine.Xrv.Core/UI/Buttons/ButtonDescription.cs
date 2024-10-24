﻿// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.UI.Buttons
{
    /// <summary>
    /// Button description.
    /// </summary>
    public class ButtonDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonDescription"/> class.
        /// </summary>
        public ButtonDescription()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets description identifier.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets a name for the button. This can help to identify button while debugging.
        /// </summary>
        public string Name { get; set; }

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
        public Func<string> TextOn { get; set; }

        /// <summary>
        /// Gets or sets text for icon for Off state in
        /// toggle buttons. Has no effect in standard buttons.
        /// </summary>
        public Func<string> TextOff { get; set; }

        /// <summary>
        /// Gets or sets voice command for standard button; or for On state in
        /// toggle buttons.
        /// </summary>
        public string VoiceCommandOn { get; set; }

        /// <summary>
        /// Gets or sets voice command for icon for Off state in
        /// toggle buttons. Has no effect in standard buttons.
        /// </summary>
        public string VoiceCommandOff { get; set; }

        /// <summary>
        /// Gets or sets button order within the hand menu.
        /// </summary>
        public int Order { get; set; }
    }
}
