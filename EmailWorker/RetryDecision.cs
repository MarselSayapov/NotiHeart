namespace EmailWorker;

public enum RetryAction
{
    Retry,
    Dead,
    Fail
}

public sealed record RetryDecision(RetryAction Action, int NextAttempt);

public static class RetryDecider
{
    public static RetryDecision Decide(bool isRetryable, int attemptNo, int maxAttempts)
    {
        var nextAttempt = attemptNo + 1;

        if (isRetryable && nextAttempt <= maxAttempts)
        {
            return new RetryDecision(RetryAction.Retry, nextAttempt);
        }

        return isRetryable
            ? new RetryDecision(RetryAction.Dead, nextAttempt)
            : new RetryDecision(RetryAction.Fail, nextAttempt);
    }
}
