namespace Linqyard.Worker.Configuration;

public sealed class IpGeolocationOptions
{
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://ip-api.com";
    public string? ApiKey { get; set; }
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);
    public bool SkipPrivateRanges { get; set; } = true;
    public double? DefaultAccuracyMeters { get; set; } = 50000;
}

public sealed class GeolocationEnrichmentOptions
{
    /// <summary>
    /// How often the enrichment job should run
    /// </summary>
    public TimeSpan RunInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// How many records to process in each batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Delay between processing each record to avoid rate limiting
    /// ip-api.com: 45 requests/min = 1.33 seconds between requests safe
    /// </summary>
    public TimeSpan DelayBetweenRecords { get; set; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Whether to process Analytics table
    /// </summary>
    public bool ProcessAnalytics { get; set; } = true;

    /// <summary>
    /// Whether to process ViewTelemetries table
    /// </summary>
    public bool ProcessViewTelemetries { get; set; } = true;
}
