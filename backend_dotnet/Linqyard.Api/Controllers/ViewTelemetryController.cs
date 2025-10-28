using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Linqyard.Api.Services;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.Extensions.Primitives;

namespace Linqyard.Api.Controllers;

public record ProfileViewPayload(
    string? Fp,
    string? Source,
    string? Referrer,
    UtmParametersDto? Utm,
    string? SessionId
);

[Route($"telemetry")]
public sealed class ViewTelemetryController(
    ILogger<ViewTelemetryController> logger,
    IViewTelemetryRepository viewTelemetryRepository,
    IUserRepository userRepository,
    IClientIpResolver clientIpResolver,
    IIpGeolocationService ipGeolocationService)
    : BaseApiController
{
    /// <summary>
    /// Record a profile view. This endpoint is intentionally permissive and does not require authentication.
    /// </summary>
    [HttpPost("/profile/{username}/view")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordProfileView( [FromRoute] string username, [FromBody] ProfileViewPayload? body, CancellationToken cancellationToken = default)
    {
        var source = body?.Source;
        try
        {
            // Get the profile user by username
            var profileUser = await userRepository.GetUserByUsernameAsync(username, cancellationToken);
            if (profileUser == null)
                return Problem(StatusCodes.Status404NotFound, "Not Found", "Profile not found");

            Guid? viewerUserId = null;
            if (IsAuthenticated && Guid.TryParse(UserId, out var parsedUserId)) viewerUserId = parsedUserId;

            // Parse source from referrer if not provided
            if (string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(body?.Referrer)) source = ParseSourceFromReferrer(body.Referrer);

            // Get user agent and parse device info
            string? userAgent = null;
            string? deviceType = null;
            string? os = null;
            string? browser = null;

            if (Request.Headers.TryGetValue("User-Agent", out var ua) && !StringValues.IsNullOrEmpty(ua)) 
                (deviceType, os, browser) = ParseUserAgent(ua.ToString());

            // Get IP address
            var ipAddress = clientIpResolver.GetClientIp(HttpContext);

            // Get location data from IP
            double? latitude = null;
            double? longitude = null;
            double? accuracy = null;
            string? city = null;
            string? country = null;

            if (ipAddress is not null)
            {
                var geo = await ipGeolocationService.ResolveAsync(ipAddress, cancellationToken);
                if (geo is not null)
                {
                    latitude = geo.Latitude;
                    longitude = geo.Longitude;
                    accuracy = geo.AccuracyMeters;
                    city = geo.City ?? geo.Region;
                    country = geo.Country;
                }
            }

            var request = new RecordProfileViewRequest(
                Id: Guid.NewGuid(),
                ProfileUserId: profileUser.Id,
                ViewerUserId: viewerUserId,
                Fingerprint: body?.Fp,
                Source: source,
                Referrer: body?.Referrer,
                UtmParameters: body?.Utm,
                Latitude: latitude,
                Longitude: longitude,
                Accuracy: accuracy,
                City: city,
                Country: country,
                UserAgent: userAgent,
                DeviceType: deviceType,
                Os: os,
                Browser: browser,
                IpAddress: ipAddress,
                SessionId: body?.SessionId,
                ViewedAt: DateTimeOffset.UtcNow
            );

            await viewTelemetryRepository.RecordProfileViewAsync(request, cancellationToken);

            return OkEnvelope(new { message = "View recorded" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error recording profile view for username {Username}", username);
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not record view");
        }
    }

    /// <summary>
    /// Get profile view telemetry for the authenticated user (paginated)
    /// </summary>
    [Authorize]
    [HttpGet("profile-views")]
    [ProducesResponseType(typeof(ApiResponse<ProfileViewsPageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileViews(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        [FromQuery] string? source,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
            {
                return Problem(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid user ID");
            }

            var request = new GetProfileViewsRequest(
                userId,
                startDate,
                endDate,
                source,
                skip,
                Math.Min(take, 100) // Cap at 100
            );

            var result = await viewTelemetryRepository.GetProfileViewsAsync(request, cancellationToken);
            return OkEnvelope(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile views");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not retrieve views");
        }
    }

    /// <summary>
    /// Get aggregated statistics for profile views
    /// </summary>
    [Authorize]
    [HttpGet("profile-stats")]
    [ProducesResponseType(typeof(ApiResponse<ProfileViewStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfileViewStats(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
            {
                return Problem(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid user ID");
            }

            var result = await viewTelemetryRepository.GetProfileViewStatsAsync(userId, startDate, endDate, cancellationToken);
            return OkEnvelope(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting profile view stats");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not retrieve stats");
        }
    }

    /// <summary>
    /// Get source breakdown for profile views
    /// </summary>
    [Authorize]
    [HttpGet($"source-breakdown")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SourceBreakdownResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSourceBreakdown(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
            {
                return Problem(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid user ID");
            }

            var result = await viewTelemetryRepository.GetSourceBreakdownAsync(userId, startDate, endDate, cancellationToken);
            return OkEnvelope(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting source breakdown");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not retrieve breakdown");
        }
    }

    /// <summary>
    /// Get geographic distribution of profile views
    /// </summary>
    [Authorize]
    [HttpGet($"geographic-distribution")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GeographicDistributionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetGeographicDistribution(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(UserId, out var userId))
            {
                return Problem(StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid user ID");
            }

            var result = await viewTelemetryRepository.GetGeographicDistributionAsync(userId, startDate, endDate, cancellationToken);
            return OkEnvelope(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting geographic distribution");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not retrieve distribution");
        }
    }

    /// <summary>
    /// Helper method to parse source from referrer URL
    /// </summary>
    private static string ParseSourceFromReferrer(string referrer)
    {
        if (string.IsNullOrEmpty(referrer))
            return "direct";

        var lowerReferrer = referrer.ToLowerInvariant();

        if (lowerReferrer.Contains("whatsapp") || lowerReferrer.Contains("wa.me"))
            return "whatsapp";
        if (lowerReferrer.Contains("twitter") || lowerReferrer.Contains("t.co"))
            return "twitter";
        if (lowerReferrer.Contains("facebook") || lowerReferrer.Contains("fb.com"))
            return "facebook";
        if (lowerReferrer.Contains("linkedin"))
            return "linkedin";
        if (lowerReferrer.Contains("instagram"))
            return "instagram";
        if (lowerReferrer.Contains("google"))
            return "google";
        if (lowerReferrer.Contains("bing"))
            return "bing";
        if (lowerReferrer.Contains("reddit"))
            return "reddit";
        if (lowerReferrer.Contains("tiktok"))
            return "tiktok";
        if (lowerReferrer.Contains("youtube"))
            return "youtube";
        if (lowerReferrer.Contains("telegram"))
            return "telegram";

        return "other";
    }

    /// <summary>
    /// Basic user agent parsing (can be enhanced with a library like UAParser)
    /// </summary>
    private static (string? deviceType, string? os, string? browser) ParseUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return (null, null, null);

        var ua = userAgent.ToLowerInvariant();

        // Device type
        string? deviceType;
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            deviceType = "mobile";
        else if (ua.Contains("tablet") || ua.Contains("ipad"))
            deviceType = "tablet";
        else
            deviceType = "desktop";

        // Operating system
        string? os = null;
        if (ua.Contains("windows"))
            os = "Windows";
        else if (ua.Contains("mac os") || ua.Contains("macos"))
            os = "macOS";
        else if (ua.Contains("linux"))
            os = "Linux";
        else if (ua.Contains("android"))
            os = "Android";
        else if (ua.Contains("ios") || ua.Contains("iphone") || ua.Contains("ipad"))
            os = "iOS";

        // Browser
        string? browser = null;
        if (ua.Contains("edg/") || ua.Contains("edge"))
            browser = "Edge";
        else if (ua.Contains("chrome") && !ua.Contains("edg"))
            browser = "Chrome";
        else if (ua.Contains("firefox"))
            browser = "Firefox";
        else if (ua.Contains("safari") && !ua.Contains("chrome"))
            browser = "Safari";
        else if (ua.Contains("opera") || ua.Contains("opr/"))
            browser = "Opera";

        return (deviceType, os, browser);
    }
}
