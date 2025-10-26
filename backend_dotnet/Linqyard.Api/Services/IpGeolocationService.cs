using System.Net;
using System.Text.Json;
using Linqyard.Api.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Linqyard.Api.Services;

public sealed class IpGeolocationService : IIpGeolocationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IpGeolocationOptions _options;
    private readonly ILogger<IpGeolocationService> _logger;

    public IpGeolocationService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<IpGeolocationOptions> options,
        ILogger<IpGeolocationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value ?? new IpGeolocationOptions();
        _logger = logger;
    }

    public async Task<IpGeolocationResult?> ResolveAsync(IPAddress? ipAddress, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || ipAddress is null)
        {
            return null;
        }

        if (_options.SkipPrivateRanges && IsPrivate(ipAddress))
        {
            return null;
        }

        var cacheKey = $"ipgeo::{ipAddress}";
        if (_cache.TryGetValue(cacheKey, out IpGeolocationResult? cached) && cached is not null)
        {
            return cached;
        }

        var requestUri = BuildRequestUri(ipAddress);
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(IpGeolocationService));
            using var response = await client.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("IP geolocation provider returned {Status} for {Ip}", response.StatusCode, ipAddress);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;

            var result = new IpGeolocationResult(
                Latitude: TryGetDouble(root, "latitude"),
                Longitude: TryGetDouble(root, "longitude"),
                City: TryGetString(root, "city"),
                Region: TryGetString(root, "region") ?? TryGetString(root, "region_name"),
                Country: TryGetString(root, "country_name") ?? TryGetString(root, "country"),
                AccuracyMeters: ResolveAccuracy(root));

            _cache.Set(cacheKey, result, _options.CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve geolocation for IP {Ip}", ipAddress);
            return null;
        }
    }

    private string BuildRequestUri(IPAddress ipAddress)
    {
        var baseUrl = _options.BaseUrl?.TrimEnd('/') ?? "https://ipapi.co";
        var uri = $"{baseUrl}/{ipAddress}/json/";
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            uri = $"{uri}?key={_options.ApiKey}";
        }
        return uri;
    }

    private double? ResolveAccuracy(JsonElement root)
    {
        var raw = TryGetDouble(root, "accuracy") ?? TryGetDouble(root, "accuracy_radius");
        if (raw.HasValue)
        {
            // ipapi returns kilometers. convert to meters when value looks like km
            if (raw.Value <= 500) // assume km
            {
                return raw.Value * 1000;
            }
            return raw.Value;
        }
        return _options.DefaultAccuracyMeters;
    }

    private static string? TryGetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? TryGetDouble(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.TryGetDouble(out var result)
            ? result
            : null;

    private static bool IsPrivate(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();
            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;
            // 169.254.0.0/16 (link local)
            if (bytes[0] == 169 && bytes[1] == 254)
                return true;
        }

        if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return ipAddress.IsIPv6LinkLocal
                || ipAddress.IsIPv6SiteLocal
                || ipAddress.IsIPv6Multicast
                || ipAddress.IsIPv6Teredo
                || ipAddress.IsIPv6UniqueLocal;
        }

        return false;
    }
}
