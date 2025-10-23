namespace Linqyard.Infra;

public sealed class RateLimitDecision
{
    public RateLimitDecision(
        string policyName,
        bool isAllowed,
        int limit,
        int count,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        DateTimeOffset timestamp,
        DateTimeOffset? retryAfterUtc = null,
        string? reason = null)
    {
        PolicyName = policyName;
        IsAllowed = isAllowed;
        Limit = limit;
        Count = count;
        Remaining = Math.Max(0, limit - count);
        WindowStart = windowStart;
        WindowEnd = windowEnd;
        Timestamp = timestamp;
        RetryAfterUtc = retryAfterUtc;
        Reason = reason;
    }

    public string PolicyName { get; }

    public bool IsAllowed { get; }

    public int Limit { get; }

    public int Count { get; }

    public int Remaining { get; }

    public DateTimeOffset WindowStart { get; }

    public DateTimeOffset WindowEnd { get; }

    public DateTimeOffset Timestamp { get; }

    public DateTimeOffset? RetryAfterUtc { get; }

    public string? Reason { get; }

    public static RateLimitDecision Allow(
        string policyName,
        int limit,
        int count,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        DateTimeOffset timestamp) =>
        new(policyName, true, limit, count, windowStart, windowEnd, timestamp);

    public static RateLimitDecision Deny(
        string policyName,
        int limit,
        int count,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        DateTimeOffset timestamp,
        DateTimeOffset retryAfterUtc,
        string? reason = null) =>
        new(policyName, false, limit, count, windowStart, windowEnd, timestamp, retryAfterUtc, reason);
}
