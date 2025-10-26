using System.Net;
using Microsoft.AspNetCore.Http;

namespace Linqyard.Api.Services;

/// <summary>
/// Resolves the real client IP address, honoring proxy headers such as X-Forwarded-For.
/// </summary>
public interface IClientIpResolver
{
    IPAddress? GetClientIp(HttpContext httpContext);
}
