// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Utils.Conditions
{
    /// <summary>
    /// Policy that will check condition by a number of times. When count is reached,
    /// it will not ask again for condition check.
    /// </summary>
    public class NumberOfTimesPolicy : IConditionCheckPolicy
    {
        /// <summary>
        /// Gets number of times condition has been satisfied. This value is reset
        /// every time condition check fails.
        /// </summary>
        public int CurrentCount { get; private set; }

        /// <summary>
        /// Gets or sets number of times condition is expected to be satisfied.
        /// </summary>
        public int NumberOfTimes { get; set; }

        /// <inheritdoc/>
        public virtual bool ShouldCheckCondition(TimeSpan timeIncrement) =>
            this.CurrentCount < this.NumberOfTimes;

        /// <inheritdoc/>
        public virtual void SetLastCheckResult(bool succeeded)
        {
            if (succeeded)
            {
                this.CurrentCount++;
            }
            else
            {
                this.Reset();
            }
        }

        /// <inheritdoc/>
        public virtual void Reset() => this.CurrentCount = 0;
    }
}
