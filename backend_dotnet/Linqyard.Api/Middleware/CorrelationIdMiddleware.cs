using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;

namespace Linqyard.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    // standard header name
    public const string HeaderName = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // 1. Try to read incoming correlation id
        string correlationId;
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues cid) &&
            !StringValues.IsNullOrEmpty(cid))
        {
            correlationId = cid.ToString();
        }
        else
        {
            // 2. If none provided, generate a new one
            correlationId = Guid.NewGuid().ToString("N");
        }

        // 3. Store it in HttpContext for downstream access
        context.Items[HeaderName] = correlationId;

        // 4. Also override TraceIdentifier (so built-in logging picks it up)
        context.TraceIdentifier = correlationId;

        // 5. Ensure the response also has the header
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
            {
                context.Response.Headers[HeaderName] = correlationId;
            }
            return Task.CompletedTask;
        });

        // 6. Call the next middleware
        await _next(context);
    }
}
