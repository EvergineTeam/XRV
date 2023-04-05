using Evergine.Xrv.Core.Utils.Conditions;
using Moq;
using System;
using Xunit;

namespace Evergine.Xrv.Core.Tests.Utils.Conditions
{
    public class TimeConditionCheckerShould
    {
        private readonly TimeConditionChecker checker;
        private readonly Mock<IConditionCheckPolicy> policy;

        public TimeConditionCheckerShould()
        {
            this.checker = new TimeConditionChecker();
            this.policy = new Mock<IConditionCheckPolicy>();
            this.policy
                .Setup(policy => policy.ShouldCheckCondition(It.IsAny<TimeSpan>()))
                .Returns(true);
            this.checker.Policy = policy.Object;
        }

        [Fact]
        public void EvaluateTrueIfCheckTimeIsZero()
        {
            this.checker.DurationTime = TimeSpan.Zero;
            Assert.True(this.checker.Check(TimeSpan.FromMilliseconds(10), () => false));
        }

        [Fact]
        public void EvaluateFalseForNonFulfilledCondition()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            Assert.False(this.checker.Check(TimeSpan.FromSeconds(0.5), () => false));
        }

        [Fact]
        public void EvaluateTrueWhenDisabled()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.IsEnabled = false;
            Assert.True(this.checker.Check(TimeSpan.FromSeconds(0.5), () => false));
        }

        [Fact]
        public void IncrementElapsedTime()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);
            Assert.Equal(0.4, this.checker.ElapsedTime.TotalSeconds);
        }

        [Fact]
        public void ResetElapsedTime()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);
            this.checker.Reset();

            Assert.Equal(0, this.checker.ElapsedTime.Seconds);
        }

        [Fact]
        public void ResetWhenConditionIsNotFulfilled()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => false);

            Assert.Equal(0, this.checker.ElapsedTime.Seconds);
        }

        [Fact]
        public void NotifyPolicyAboutConditionResultWhenRequired()
        {
            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);

            policy.Verify(p => p.SetLastCheckResult(true), Times.Once);
        }

        [Fact]
        public void NotNotifyPolicyAboutConditionResultWhenNotRequired()
        {
            policy
                .Setup(policy => policy.ShouldCheckCondition(It.IsAny<TimeSpan>()))
                .Returns(false);

            this.checker.DurationTime = TimeSpan.FromSeconds(1);
            this.checker.Check(TimeSpan.FromSeconds(0.2), () => true);

            policy.Verify(p => p.SetLastCheckResult(true), Times.Never);
        }

        [Fact]
        public void ResetPolicy()
        {
            this.checker.Reset();
            policy.Verify(policy => policy.Reset(), Times.Once);
        }
    }
}
