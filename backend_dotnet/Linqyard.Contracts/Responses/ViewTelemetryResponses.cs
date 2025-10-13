using System.Net;

namespace Linqyard.Contracts.Responses;

/// <summary>
/// Response containing a single profile view telemetry record
/// </summary>
public record ProfileViewResponse(
    Guid Id,
    Guid ProfileUserId,
    Guid? ViewerUserId,
    string? ViewerUsername,
    string? Fingerprint,
    string? Source,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    double? Latitude,
    double? Longitude,
    double? Accuracy,
    string? City,
    string? Country,
    string? UserAgent,
    string? DeviceType,
    string? Os,
    string? Browser,
    string? IpAddress,
    string? SessionId,
    int? DurationSeconds,
    DateTimeOffset ViewedAt
);

/// <summary>
/// Aggregated statistics for profile views
/// </summary>
public record ProfileViewStatsResponse(
    long TotalViews,
    long UniqueVisitors,
    Dictionary<string, long> ViewsBySource,
    Dictionary<string, long> ViewsByCountry,
    Dictionary<string, long> ViewsByDevice,
    List<DailyViewCount> DailyViews,
    double AverageViewDuration
);

/// <summary>
/// Daily view count for trend analysis
/// </summary>
public record DailyViewCount(
    DateOnly Date,
    long Count
);

/// <summary>
/// Paginated response for profile views
/// </summary>
public record ProfileViewsPageResponse(
    List<ProfileViewResponse> Views,
    int Total,
    int Skip,
    int Take
);

/// <summary>
/// Source breakdown with percentage
/// </summary>
public record SourceBreakdownResponse(
    string Source,
    long Count,
    double Percentage
);

/// <summary>
/// Geographic distribution of views
/// </summary>
public record GeographicDistributionResponse(
    string Country,
    string? City,
    long Count,
    double Percentage
);
