using Evergine.Xrv.Core.Utils.Conditions;
using System;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Utils.Conditions
{
    public class NumberOfTimesPolicyShould
    {
        private readonly NumberOfTimesPolicy policy;

        public NumberOfTimesPolicyShould()
        {
            this.policy = new NumberOfTimesPolicy();
        }

        [Fact]
        public void RequireCheckConditionIfNumberOfTimesNotReached()
        {
            policy.NumberOfTimes = 3;

            for (int i = 0; i < policy.NumberOfTimes - 1; i++)
            {
                policy.SetLastCheckResult(true);
            }

            Assert.True(policy.ShouldCheckCondition(default));
        }

        [Fact]
        public void NotRequireCheckConditionIfNumberOfTimesHasBeenReached()
        {
            policy.NumberOfTimes = 3;

            for (int i = 0; i < policy.NumberOfTimes; i++)
            {
                policy.SetLastCheckResult(true);
            }

            Assert.False(policy.ShouldCheckCondition(default));
        }

        [Fact]
        public void ResetCounterWhenConditionIsNotFulfilled()
        {
            policy.NumberOfTimes = 3;
            policy.SetLastCheckResult(true);
            policy.SetLastCheckResult(true);
            policy.SetLastCheckResult(false);

            Assert.Equal(0, policy.CurrentCount);
        }
    }
}
