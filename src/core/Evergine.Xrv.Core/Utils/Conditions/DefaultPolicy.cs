// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Utils.Conditions
{
    /// <summary>
    /// Default policy. It will continuously require condition checks.
    /// </summary>
    public class DefaultPolicy : IConditionCheckPolicy
    {
        static DefaultPolicy()
        {
            Instance = new DefaultPolicy();
        }

        private DefaultPolicy()
        {
        }

        /// <summary>
        /// Gets policy instance.
        /// </summary>
        public static DefaultPolicy Instance { get; private set; }

        /// <inheritdoc/>
        public bool ShouldCheckCondition(TimeSpan timeIncrement) => true;

        /// <inheritdoc/>
        public void SetLastCheckResult(bool succeeded)
        {
        }

        /// <inheritdoc/>
        public void Reset()
        {
        }
    }
}
