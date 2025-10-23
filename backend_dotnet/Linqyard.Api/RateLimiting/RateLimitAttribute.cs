using System.Globalization;
using System.Security.Claims;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Linqyard.Api.RateLimiting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    public RateLimitAttribute(string policyName)
    {
        PolicyName = policyName ?? throw new ArgumentNullException(nameof(policyName));
    }

    public string PolicyName { get; }

    public RateLimitPartitionStrategy Partition { get; init; } = RateLimitPartitionStrategy.IpAddress;

    public string? HeaderName { get; init; }

    public string? ClaimType { get; init; }

    public string? FixedKey { get; init; }

    public string? RouteValueKey { get; init; }

    public bool FallbackToIp { get; init; } = true;

    public bool RequireAuthenticatedUser { get; init; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var rateLimiter = httpContext.RequestServices.GetService<IRateLimiterService>();

        if (rateLimiter is null)
        {
            await next().ConfigureAwait(false);
            return;
        }

        var key = ResolveKey(context);
        if (string.IsNullOrWhiteSpace(key))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var decision = await rateLimiter.ShouldAllowAsync(PolicyName, key, cancellationToken).ConfigureAwait(false);
        RateLimitResponseExtensions.ApplyRateLimitHeaders(httpContext.Response, decision);

        if (!decision.IsAllowed)
        {
            context.Result = CreateTooManyRequestsResult(decision);
            return;
        }

        await next().ConfigureAwait(false);
        RateLimitResponseExtensions.ApplyRateLimitHeaders(httpContext.Response, decision);
    }

    private string? ResolveKey(ActionExecutingContext context)
    {
        var httpContext = context.HttpContext;
        var partition = Partition;
        string? resolved = partition switch
        {
            RateLimitPartitionStrategy.IpAddress => httpContext.Connection.RemoteIpAddress?.ToString(),
            RateLimitPartitionStrategy.UserId => ResolveUserIdentifier(httpContext, RequireAuthenticatedUser),
            RateLimitPartitionStrategy.Header => ResolveHeader(httpContext, HeaderName),
            RateLimitPartitionStrategy.Claim => ResolveClaim(httpContext, ClaimType),
            RateLimitPartitionStrategy.RouteValue => ResolveRouteValue(context, RouteValueKey),
            RateLimitPartitionStrategy.Fixed => FixedKey,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(resolved) && FallbackToIp && partition is not RateLimitPartitionStrategy.IpAddress)
        {
            resolved = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        return string.IsNullOrWhiteSpace(resolved) ? null : resolved.Trim();
    }

    private static string? ResolveHeader(HttpContext context, string? headerName) =>
        string.IsNullOrWhiteSpace(headerName)
            ? null
            : context.Request.Headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.ToString()
                : null;

    private static string? ResolveClaim(HttpContext context, string? claimType)
    {
        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(claimType))
        {
            claimType = ClaimTypes.NameIdentifier;
        }

        return principal.FindFirst(claimType)?.Value;
    }

    private static string? ResolveUserIdentifier(HttpContext context, bool requireAuthenticated)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return requireAuthenticated ? null : context.Connection.RemoteIpAddress?.ToString();
        }

        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? context.User.FindFirst("oid")?.Value
               ?? context.User.Identity?.Name
               ?? context.Connection.RemoteIpAddress?.ToString();
    }

    private static string? ResolveRouteValue(ActionExecutingContext context, string? routeValueKey)
    {
        if (string.IsNullOrWhiteSpace(routeValueKey))
        {
            return null;
        }

        if (!context.RouteData.Values.TryGetValue(routeValueKey, out var value) || value is null)
        {
            return null;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static IActionResult CreateTooManyRequestsResult(RateLimitDecision decision)
    {
        var detail = decision.Reason ?? "Too many requests. Please try again later.";
        var problem = new ProblemDetails
        {
            Title = "Too Many Requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = detail,
            Type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            Instance = null
        };

        problem.Extensions["policy"] = decision.PolicyName;
        problem.Extensions["limit"] = decision.Limit;
        problem.Extensions["windowStart"] = decision.WindowStart;
        problem.Extensions["windowEnd"] = decision.WindowEnd;
        problem.Extensions["retryAfterUtc"] = decision.RetryAfterUtc;

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
        };
    }
}

public enum RateLimitPartitionStrategy
{
    IpAddress,
    UserId,
    Header,
    Claim,
    RouteValue,
    Fixed
}
