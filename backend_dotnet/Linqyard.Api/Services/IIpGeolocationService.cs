using System.Net;

namespace Linqyard.Api.Services;

public interface IIpGeolocationService
{
    Task<IpGeolocationResult?> ResolveAsync(IPAddress? ipAddress, CancellationToken cancellationToken = default);
}

public sealed record IpGeolocationResult(
    double? Latitude,
    double? Longitude,
    string? City,
    string? Region,
    string? Country,
    double? AccuracyMeters);
