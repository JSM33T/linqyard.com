using Linqyard.Api.Extensions;
using Linqyard.Contracts;
using Linqyard.Infra;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace Linqyard.Api.Controllers;

/// <summary>
/// Base API controller providing common helpers for request context,
/// standardized ProblemDetails responses (RFC 7807), and response envelopes.
/// </summary>
/// <remarks>
/// This controller is hidden from Swagger by default and should be inherited
/// by concrete API controllers.
/// </remarks>
[ApiController]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Standard request ID header name.
    /// </summary>
    private const string HeaderRequestId = "X-Request-Id";

    /// <summary>
    /// Standard correlation ID header name.
    /// </summary>
    private const string HeaderCorrelationId = "X-Correlation-Id";

    // -------- Context helpers --------

    /// <summary>
    /// Gets the current request ID from <see cref="HttpContext.Items"/> or headers.
    /// </summary>
    protected string? RequestId =>
        HttpContext.Items[nameof(RequestId)] as string
        ?? TryGetHeader(HeaderRequestId);

    /// <summary>
    /// Gets the current correlation ID from <see cref="HttpContext.Items"/> or headers.
    /// </summary>
    protected string? CorrelationId =>
        HttpContext.Items[nameof(CorrelationId)] as string
        ?? TryGetHeader(HeaderCorrelationId);

    /// <summary>
    /// Gets the bearer token (without the "Bearer " prefix) from the Authorization header.
    /// </summary>
    protected string? BearerToken =>
        Request.Headers.TryGetValue(HeaderNames.Authorization, out var auth) &&
        auth.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? auth.ToString()["Bearer ".Length..]
            : null;

    /// <summary>
    /// Gets the user identifier from claims (NameIdentifier or "oid").
    /// </summary>
    protected string? UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("oid")?.Value;

    /// <summary>
    /// Gets the tenant identifier from claims ("tid" or "tenant_id").
    /// </summary>
    protected string? TenantId =>
        User.FindFirst("tid")?.Value
        ?? User.FindFirst("tenant_id")?.Value;

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    // -------- ProblemDetails helpers (RFC 7807) --------

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 404 Not Found.
    /// </summary>
    protected IActionResult NotFoundProblem(string title = "Resource not found", string? detail = null)
        => Problem(StatusCodes.Status404NotFound, title, detail);

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 400 Bad Request.
    /// </summary>
    protected IActionResult BadRequestProblem(string title = "Bad request", string? detail = null)
        => Problem(StatusCodes.Status400BadRequest, title, detail);

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 409 Conflict.
    /// </summary>
    protected IActionResult ConflictProblem(string title = "Conflict", string? detail = null)
        => Problem(StatusCodes.Status409Conflict, title, detail);

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 403 Forbidden.
    /// </summary>
    protected IActionResult ForbiddenProblem(string title = "Forbidden", string? detail = null)
        => Problem(StatusCodes.Status403Forbidden, title, detail);

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 401 Unauthorized.
    /// </summary>
    protected IActionResult UnauthorizedProblem(string title = "Unauthorized", string? detail = null)
        => Problem(StatusCodes.Status401Unauthorized, title, detail);

    /// <summary>
    /// Returns a <see cref="ProblemDetails"/> response with 429 Too Many Requests (RFC 6585).
    /// Applies standard rate-limit headers when a decision is provided.
    /// </summary>
    protected IActionResult TooManyRequestsProblem(
        string title = "Too many requests",
        string? detail = null,
        RateLimitDecision? decision = null)
    {
        if (decision is not null)
        {
            ApplyRateLimitHeaders(decision);
        }

        IDictionary<string, object?>? extensions = null;
        if (decision is not null)
        {
            extensions = new Dictionary<string, object?>
            {
                ["policy"] = decision.PolicyName,
                ["limit"] = decision.Limit,
                ["windowStart"] = decision.WindowStart,
                ["windowEnd"] = decision.WindowEnd,
                ["retryAfterUtc"] = decision.RetryAfterUtc
            };
        }

        var message = detail ?? decision?.Reason ?? "Rate limit exceeded. Please try again later.";

        return Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: title,
            detail: message,
            type: "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            instance: null,
            extensions: extensions);
    }

    /// <summary>
    /// Creates a <see cref="ProblemDetails"/> response with extensions.
    /// </summary>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="title">Title of the problem.</param>
    /// <param name="detail">Optional detail message.</param>
    /// <param name="type">Problem type URI (default is "about:blank").</param>
    /// <param name="instance">Optional instance URI (defaults to request path).</param>
    /// <param name="extensions">Additional key-value pairs added to extensions.</param>
    protected IActionResult Problem(
        int statusCode,
        string title,
        string? detail = null,
        string? type = "about:blank",
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        var pd = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = instance ?? HttpContext.Request.Path
        };

        if (extensions is not null)
        {
            foreach (var kvp in extensions)
                pd.Extensions[kvp.Key] = kvp.Value;
        }

        // Always echo correlation/request IDs if present
        if (!string.IsNullOrWhiteSpace(RequestId)) pd.Extensions[nameof(RequestId)] = RequestId;
        if (!string.IsNullOrWhiteSpace(CorrelationId)) pd.Extensions[nameof(CorrelationId)] = CorrelationId;

        return new ObjectResult(pd)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary>
    /// Applies standardized rate-limit headers to the current response.
    /// </summary>
    /// <param name="decision">Rate-limit evaluation result.</param>
    protected void ApplyRateLimitHeaders(RateLimitDecision decision)
    {
        RateLimitResponseExtensions.ApplyRateLimitHeaders(Response, decision);
    }

    // -------- Optional success envelope helpers --------

    /// <summary>
    /// Returns a 200 OK response with a standardized <see cref="ApiResponse{T}"/>.
    /// </summary>
    protected IActionResult OkEnvelope<T>(T data, object? meta = null)
        => Ok(new ApiResponse<T>(data, meta));

    /// <summary>
    /// Returns a 201 Created response with a standardized <see cref="ApiResponse{T}"/>.
    /// </summary>
    protected IActionResult CreatedEnvelope<T>(string routeName, object routeValues, T data, object? meta = null)
        => CreatedAtRoute(routeName, routeValues, new ApiResponse<T>(data, meta));

    /// <summary>
    /// Returns a 200 OK response with a paged <see cref="ApiResponse{T}"/>.
    /// </summary>
    protected IActionResult PagedOk<T>(IReadOnlyCollection<T> items, int page, int pageSize, long total)
        => Ok(new ApiResponse<IReadOnlyCollection<T>>(items, new PagedMeta(page, pageSize, total)));

    // -------- Utilities --------

    /// <summary>
    /// Tries to get a header value as string if present and not empty.
    /// </summary>
    private string? TryGetHeader(string name)
        => Request.Headers.TryGetValue(name, out StringValues v) && !StringValues.IsNullOrEmpty(v)
            ? v.ToString()
            : null;
}
