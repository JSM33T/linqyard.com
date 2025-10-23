namespace Linqyard.Infra;

public interface IRateLimiterService
{
    Task<RateLimitDecision> ShouldAllowAsync(
        string policyName,
        string key,
        CancellationToken cancellationToken = default);
}
