namespace Linqyard.Api.Configuration;

public sealed class IpGeolocationOptions
{
    /// <summary>
    /// Enables server-side lookup. When false, the resolver short-circuits.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Base URL of the geolocation provider. Defaults to ipapi.co.
    /// </summary>
    public string BaseUrl { get; set; } = "https://ipapi.co";

    /// <summary>
    /// Optional API key (depends on provider plan).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Cache duration for resolved IPs.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to skip private/reserved IP ranges.
    /// </summary>
    public bool SkipPrivateRanges { get; set; } = true;

    /// <summary>
    /// Fallback accuracy radius in meters when the provider does not supply one.
    /// </summary>
    public double? DefaultAccuracyMeters { get; set; } = 50000; // 50km default
}
