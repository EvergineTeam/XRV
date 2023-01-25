// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using Evergine.Common.Attributes;
using Evergine.Framework;
using System;
using Xrv.Core.Services.Messaging;

namespace Xrv.Core.Localization
{
    /// <summary>
    /// Base localization class.
    /// </summary>
    public abstract class BaseLocalization : Component
    {
        private string dictionaryName;
        private string dictionaryKey;
        private Func<string> localizationFunc;

        [BindService]
        private LocalizationService localization = null;

        [BindService]
        private XrvService xrvService = null;

        private bool textLoaded;
        private PubSub pubSub;
        private Guid subscription;

        /// <summary>
        /// Raised when <see cref="DictionaryName"/> is changed.
        /// </summary>
        public event EventHandler DictionaryNameChanged;

        /// <summary>
        /// Gets or sets localization dictionary name.
        /// </summary>
        public string DictionaryName
        {
            get => this.dictionaryName;
            set
            {
                if (this.dictionaryName != value)
                {
                    this.dictionaryName = value;
                    this.OnDictionaryNameUpdated();
                }
            }
        }

        /// <summary>
        /// Gets or sets dictionary localization key.
        /// </summary>
        public string DictionaryKey
        {
            get => this.dictionaryKey;
            set
            {
                if (this.dictionaryKey != value)
                {
                    this.dictionaryKey = value;
                    this.RequestUpdate();
                }
            }
        }

        /// <summary>
        /// Gets or sets localization function for text, instead of explicit
        /// <see cref="DictionaryName"/> and <see cref="DictionaryKey"/>.
        /// </summary>
        [IgnoreEvergine]
        public Func<string> LocalizationFunc
        {
            get => this.localizationFunc;
            set
            {
                if (this.localizationFunc != value)
                {
                    this.localizationFunc = value;
                    this.DictionaryName = null;
                }
            }
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            bool attached = base.OnAttached();
            if (attached)
            {
                this.pubSub = this.xrvService.Services.Messaging;
                this.subscription = this.pubSub.Subscribe<CurrentCultureChangeMessage>(this.OnCultureChange);
            }

            return attached;
        }

        /// <inheritdoc/>
        protected override void OnDetach()
        {
            base.OnDetach();
            this.pubSub.Unsubscribe(this.subscription);
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();

            if (this.ShouldUpdateText())
            {
                this.RequestUpdate();
            }
        }

        /// <summary>
        /// Evaluates if text should be updated.
        /// </summary>
        /// <returns>True if text should be updated; false otherwise.</returns>
        protected virtual bool ShouldUpdateText() => !this.textLoaded;

        /// <summary>
        /// Invoked when text is required to be updated.
        /// </summary>
        /// <param name="text">New text.</param>
        protected abstract void SetText(string text);

        /// <summary>
        /// Forces a text update.
        /// </summary>
        protected void RequestUpdate()
        {
            if (this.localizationFunc != null)
            {
                this.SetText(this.localizationFunc.Invoke());
                return;
            }

            if (this.dictionaryName == null || this.dictionaryKey == null)
            {
                return;
            }

            var text = this.localization.GetString(this.dictionaryName, this.dictionaryKey);
            this.SetText(text);
        }

        private void OnDictionaryNameUpdated()
        {
            if (this.IsAttached)
            {
                this.DictionaryKey = null;
            }

            this.DictionaryNameChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCultureChange(CurrentCultureChangeMessage message)
        {
            if (this.IsActivated)
            {
                this.RequestUpdate();
            }
            else
            {
                this.textLoaded = false;
            }
        }
    }
}
