namespace Linqyard.Infra.Configuration;

public sealed class RateLimitOptions
{
    public RateLimitPolicy? DefaultPolicy { get; set; }

    public IDictionary<string, RateLimitPolicy> Policies { get; set; }
        = new Dictionary<string, RateLimitPolicy>(StringComparer.OrdinalIgnoreCase);

    public bool ThrowOnMissingPolicy { get; set; } = true;

    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
}
