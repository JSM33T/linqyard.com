using System.Net;

namespace Linqyard.Contracts.Requests;

/// <summary>
/// Request to record a profile view
/// </summary>
public record RecordProfileViewRequest(
    Guid Id,
    Guid ProfileUserId,
    Guid? ViewerUserId,
    string? Fingerprint,
    string? Source,
    string? Referrer,
    UtmParametersDto? UtmParameters,
    double? Latitude,
    double? Longitude,
    double? Accuracy,
    string? City,
    string? Country,
    string? UserAgent,
    string? DeviceType,
    string? Os,
    string? Browser,
    IPAddress? IpAddress,
    string? SessionId,
    DateTimeOffset ViewedAt
);

/// <summary>
/// UTM Parameters for campaign tracking
/// </summary>
public record UtmParametersDto(
    string? Source,      // utm_source
    string? Medium,      // utm_medium
    string? Campaign,    // utm_campaign
    string? Term,        // utm_term
    string? Content      // utm_content
);

/// <summary>
/// Request to get profile view telemetry with filters
/// </summary>
public record GetProfileViewsRequest(
    Guid ProfileUserId,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    string? Source,
    int Skip,
    int Take
);
