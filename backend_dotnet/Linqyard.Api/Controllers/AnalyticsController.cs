using System.Net;
using Linqyard.Api.Services;
using Linqyard.Contracts;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace Linqyard.Api.Controllers;

public record ClickPayload(string? fp);

[Route("analytics")]
public sealed class AnalyticsController : BaseApiController
{
    private readonly ILogger<AnalyticsController> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IClientIpResolver _clientIpResolver;
    private readonly IIpGeolocationService _ipGeolocationService;

    public AnalyticsController(
        ILogger<AnalyticsController> logger,
        IUserRepository userRepository,
        IAnalyticsRepository analyticsRepository,
        IClientIpResolver clientIpResolver,
        IIpGeolocationService ipGeolocationService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _analyticsRepository = analyticsRepository;
        _clientIpResolver = clientIpResolver;
        _ipGeolocationService = ipGeolocationService;
    }

    /// <summary>
    /// Record a click for a link. This endpoint is intentionally permissive and does not require authentication.
    /// If the request is authenticated, the UserId will be set on the AuditLog so users can later query their own analytics.
    /// </summary>
    [HttpPost("/link/{linkId:guid}/click")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordLinkClick([FromRoute] Guid linkId, [FromBody] ClickPayload? body, CancellationToken cancellationToken = default)
    {
        try
        {
            Guid? userId = null;
            if (IsAuthenticated && Guid.TryParse(UserId, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            string? userAgent = null;
            if (Request.Headers.TryGetValue("User-Agent", out var ua) && !StringValues.IsNullOrEmpty(ua))
            {
                userAgent = ua.ToString();
            }

            IPAddress? ipAddress = _clientIpResolver.GetClientIp(HttpContext);

            double? latitude = null;
            double? longitude = null;
            double? accuracy = null;

            if (ipAddress is not null)
            {
                var geo = await _ipGeolocationService.ResolveAsync(ipAddress, cancellationToken);
                if (geo is not null)
                {
                    latitude = geo.Latitude;
                    longitude = geo.Longitude;
                    accuracy = geo.AccuracyMeters;
                }
            }

            var request = new RecordLinkClickRequest(
                Id: Guid.NewGuid(),
                LinkId: linkId,
                UserId: userId,
                Fingerprint: body?.fp,
                Latitude: latitude,
                Longitude: longitude,
                Accuracy: accuracy,
                UserAgent: userAgent,
                IpAddress: ipAddress,
                At: DateTimeOffset.UtcNow);

            await _analyticsRepository.RecordLinkClickAsync(request, cancellationToken);

            return OkEnvelope(new { message = "Recorded" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording link click");
            return Problem(StatusCodes.Status500InternalServerError, "Internal Server Error", "Could not record click");
        }
    }

    /// <summary>
    /// Returns analytics counts grouped by the authenticated user's links.
    /// Only the owner may call this endpoint.
    /// </summary>
    [HttpGet("my/links")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLinkCounts(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || !Guid.TryParse(UserId, out var uid)) return UnauthorizedProblem();

        var results = await _analyticsRepository.GetLinkClickCountsAsync(uid, cancellationToken);
        return OkEnvelope(results);
    }

    /// <summary>
    /// Returns recent analytics events for a specific link owned by the authenticated user.
    /// </summary>
    [HttpGet("my/link/{linkId:guid}/events")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLinkEvents([FromRoute] Guid linkId, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || !Guid.TryParse(UserId, out var uid)) return UnauthorizedProblem();

        var events = await _analyticsRepository.GetLinkEventsForUserAsync(uid, linkId, 100, cancellationToken);
        if (events is null) return ForbiddenProblem("You do not own this link");

        return OkEnvelope(events);
    }

    /// <summary>
    /// Returns summary metrics (total clicks and average clicks per day) for the authenticated user's links in a date range.
    /// </summary>
    [HttpGet("my/summary")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySummary([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || !Guid.TryParse(UserId, out var uid)) return UnauthorizedProblem();

        var toVal = to ?? DateTimeOffset.UtcNow;
        var fromVal = from ?? toVal.AddDays(-29);

        var startDateUtc = new DateTime(fromVal.Year, fromVal.Month, fromVal.Day, 0, 0, 0, DateTimeKind.Utc);
        var endDateUtc = new DateTime(toVal.Year, toVal.Month, toVal.Day, 23, 59, 59, DateTimeKind.Utc).AddMilliseconds(999);

        var start = new DateTimeOffset(startDateUtc, TimeSpan.Zero);
        var end = new DateTimeOffset(endDateUtc, TimeSpan.Zero);

        var totalClicks = await _analyticsRepository.GetClickCountAsync(uid, start, end, cancellationToken);

        var days = (toVal.Date - fromVal.Date).TotalDays + 1;
        var averagePerDay = days > 0 ? (double)totalClicks / days : 0.0;

        var result = new
        {
            totalClicks,
            averagePerDay,
            days = (int)days,
            from = start,
            to = toVal
        };

        return OkEnvelope(result);
    }

    /// <summary>
    /// Get the total number of users in the system
    /// </summary>
    [HttpGet("users/count")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserCount(CancellationToken cancellationToken = default)
    {
        try
        {
            var userCount = await _userRepository.GetUserCountAsync(cancellationToken);

            var result = new
            {
                totalUsers = userCount,
                retrievedAt = DateTimeOffset.UtcNow
            };

            return OkEnvelope(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Configuration error while retrieving user count");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Configuration Error",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user count");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An error occurred while retrieving user count");
        }
    }

    /// <summary>
    /// Returns counts of clicks grouped by device type (Desktop, Mobile, Tablet, Other)
    /// for the authenticated user's links.
    /// </summary>
    [HttpGet("my/devices")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyDevices(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated || !Guid.TryParse(UserId, out var uid)) return UnauthorizedProblem();

        var userAgents = await _analyticsRepository.GetUserAgentsForUserAsync(uid, cancellationToken);

        int mobile = 0, tablet = 0, desktop = 0, other = 0;

        foreach (var ua in userAgents)
        {
            if (string.IsNullOrWhiteSpace(ua))
            {
                other++;
                continue;
            }

            var lower = ua.ToLowerInvariant();

            if (lower.Contains("ipad") || (lower.Contains("android") && lower.Contains("tablet")) || lower.Contains("tablet"))
            {
                tablet++;
                continue;
            }

            if (lower.Contains("mobi") || lower.Contains("iphone") || (lower.Contains("android") && !lower.Contains("tablet")) || lower.Contains("phone"))
            {
                mobile++;
                continue;
            }

            if (lower.Contains("windows") || lower.Contains("macintosh") || lower.Contains("linux") || lower.Contains("x11") || lower.Contains("cros"))
            {
                desktop++;
                continue;
            }

            other++;
        }

        var result = new
        {
            desktop,
            mobile,
            tablet,
            other,
            total = desktop + mobile + tablet + other,
            retrievedAt = DateTimeOffset.UtcNow
        };

        return OkEnvelope(result);
    }
}
