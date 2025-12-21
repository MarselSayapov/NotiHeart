using EmailWorker;
using Xunit;

namespace NotiHeart.Tests;

public sealed class RetryDeciderTests
{
    [Fact]
    public void RetryableUnderLimit_ReturnsRetry()
    {
        var decision = RetryDecider.Decide(isRetryable: true, attemptNo: 1, maxAttempts: 5);

        Assert.Equal(RetryAction.Retry, decision.Action);
        Assert.Equal(2, decision.NextAttempt);
    }

    [Fact]
    public void RetryableOverLimit_ReturnsDead()
    {
        var decision = RetryDecider.Decide(isRetryable: true, attemptNo: 5, maxAttempts: 5);

        Assert.Equal(RetryAction.Dead, decision.Action);
        Assert.Equal(6, decision.NextAttempt);
    }

    [Fact]
    public void NonRetryable_ReturnsFail()
    {
        var decision = RetryDecider.Decide(isRetryable: false, attemptNo: 1, maxAttempts: 5);

        Assert.Equal(RetryAction.Fail, decision.Action);
        Assert.Equal(2, decision.NextAttempt);
    }
}
