// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace Evergine.Xrv.Core.Utils.Conditions
{
    /// <summary>
    /// Condition check policy for <see cref="TimeConditionChecker"/>.
    /// </summary>
    public interface IConditionCheckPolicy
    {
        /// <summary>
        /// Checks if checker condition should be evaluated by a given time
        /// increment.
        /// </summary>
        /// <param name="timeIncrement">Time increment since last check.</param>
        /// <returns>True if condition should be evaluated; false otherwise.</returns>
        bool ShouldCheckCondition(TimeSpan timeIncrement);

        /// <summary>
        /// Invoked by checker to indicate result of last condition check.
        /// </summary>
        /// <param name="succeeded">Last condition check result.</param>
        void SetLastCheckResult(bool succeeded);

        /// <summary>
        /// Resets policy state.
        /// </summary>
        void Reset();
    }
}
