using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public interface IRateLimiterService
{
    Task<RateLimitDecision> ShouldAllowAsync(
        string policyName,
        string key,
        CancellationToken cancellationToken = default);
}
