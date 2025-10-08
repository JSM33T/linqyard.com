using System;

namespace Linqyard.Contracts.Responses;

public record LinkClickCountResponse(Guid LinkId, long Clicks);

public record AnalyticsEventResponse(
    Guid Id,
    DateTimeOffset At,
    string? Fingerprint,
    double? Latitude,
    double? Longitude,
    double? Accuracy,
    string? UserAgent);
