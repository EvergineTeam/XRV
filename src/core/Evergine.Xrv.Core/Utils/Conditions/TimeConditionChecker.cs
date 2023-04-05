// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Utils.Conditions
{
    /// <summary>
    /// Time-based checker that ensures a condition is fulfilled during its configured
    /// time.
    /// </summary>
    public class TimeConditionChecker
    {
        private bool isEnabled = true;
        private IConditionCheckPolicy policy = DefaultPolicy.Instance;

        /// <summary>
        /// Gets current elapsed time.
        /// </summary>
        public TimeSpan ElapsedTime { get; private set; }

        /// <summary>
        /// Gets or sets checks duration time.
        /// </summary>
        public TimeSpan DurationTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether checks are in progress.
        /// </summary>
        public bool IsInProgress { get => this.ElapsedTime < this.DurationTime; }

        /// <summary>
        /// Gets or sets a value indicating whether checker is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => this.isEnabled;
            set
            {
                if (this.isEnabled != value)
                {
                    this.isEnabled = value;
                    if (this.AutomaticResetWhenDisabled && !this.isEnabled)
                    {
                        this.Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether checker should be reset when
        /// it is disabled.
        /// </summary>
        public bool AutomaticResetWhenDisabled { get; set; } = true;

        /// <summary>
        /// Gets or sets condition check policy. Defaults to <see cref="DefaultPolicy"/>.
        /// </summary>
        public IConditionCheckPolicy Policy
        {
            get => this.policy;

            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException($"{nameof(this.Policy)} can't be null");
                }

                this.policy = value;
            }
        }

        /// <summary>
        /// Checks that condition is satisfied by given time increment. Condition evaluation
        /// will be performed or not depending on current policy state.
        /// </summary>
        /// <param name="timeIncrement">Time increment since last check invoke.</param>
        /// <param name="condition">Condition to be evaluated.</param>
        /// <returns>True if checker has been successfully completed.</returns>
        public bool Check(TimeSpan timeIncrement, Func<bool> condition)
        {
            if (!this.IsEnabled)
            {
                return true;
            }

            this.ElapsedTime += timeIncrement;

            if (this.policy.ShouldCheckCondition(timeIncrement))
            {
                bool result = condition.Invoke();
                this.policy.SetLastCheckResult(result);

                if (!result)
                {
                    this.Reset();
                }
            }

            return !this.IsInProgress;
        }

        /// <summary>
        /// Resets checker state.
        /// </summary>
        public void Reset()
        {
            this.ElapsedTime = TimeSpan.Zero;
            this.Policy.Reset();
        }
    }
}
