// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

namespace Evergine.Xrv.Core.UI.Dialogs
{
    /// <summary>
    /// Model for dialog option.
    /// </summary>
    public class DialogOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogOption"/> class.
        /// </summary>
        /// <param name="key">Option key. It should be unique in dialog to work properly.</param>
        public DialogOption(string key)
        {
            this.Key = key;
            this.Configuration = new OptionButtonConfigurator
            {
                AllowBackPlateNullMaterial = true,
            };
        }

        /// <summary>
        /// Gets option key.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets option button configuration.
        /// </summary>
        public OptionButtonConfigurator Configuration { get; private set; }
    }
}
