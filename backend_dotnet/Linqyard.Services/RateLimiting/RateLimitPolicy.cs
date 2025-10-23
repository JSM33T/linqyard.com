namespace Linqyard.Services.RateLimiting;

public sealed class RateLimitPolicy
{
    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 60;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    public string? Description { get; set; }

    public bool IsActive =>
        Enabled &&
        PermitLimit > 0 &&
        Window > TimeSpan.Zero;
}
