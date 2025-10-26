using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Linqyard.Api.Services;

/// <summary>
/// Default implementation that inspects common forwarding headers before falling back to the connection address.
/// </summary>
public sealed class ClientIpResolver : IClientIpResolver
{
    private static readonly string[] ForwardingHeaders =
    {
        "X-Forwarded-For",
        "X-Real-IP",
        "CF-Connecting-IP",
        "True-Client-IP"
    };

    private readonly ILogger<ClientIpResolver> _logger;

    public ClientIpResolver(ILogger<ClientIpResolver> logger)
    {
        _logger = logger;
    }

    public IPAddress? GetClientIp(HttpContext httpContext)
    {
        foreach (var header in ForwardingHeaders)
        {
            if (!httpContext.Request.Headers.TryGetValue(header, out var values) || values.Count == 0)
            {
                continue;
            }

            var raw = values.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var candidate = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate) && IPAddress.TryParse(candidate, out var parsed))
            {
                return parsed;
            }

            _logger.LogDebug("Failed to parse IP from header {Header} value {Value}", header, raw);
        }

        return httpContext.Connection.RemoteIpAddress;
    }
}
