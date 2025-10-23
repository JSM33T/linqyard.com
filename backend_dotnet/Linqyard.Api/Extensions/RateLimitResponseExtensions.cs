using System.Globalization;
using Linqyard.Contracts.Responses;
using Linqyard.Infra;
using Microsoft.AspNetCore.Http;

namespace Linqyard.Api.Extensions;

internal static class RateLimitResponseExtensions
{
    private const string HeaderLimit = "X-RateLimit-Limit";
    private const string HeaderRemaining = "X-RateLimit-Remaining";
    private const string HeaderReset = "X-RateLimit-Reset";
    private const string HeaderPolicy = "X-RateLimit-Policy";
    private const string HeaderRetryAfter = "Retry-After";

    internal static void ApplyRateLimitHeaders(HttpResponse response, RateLimitDecision decision)
    {
        var headers = response.Headers;
        headers[HeaderPolicy] = decision.PolicyName;
        headers[HeaderLimit] = decision.Limit.ToString(CultureInfo.InvariantCulture);
        headers[HeaderRemaining] = Math.Max(0, decision.Remaining).ToString(CultureInfo.InvariantCulture);

        var resetSeconds = Math.Max(
            0,
            (int)Math.Ceiling((decision.WindowEnd - decision.Timestamp).TotalSeconds));
        headers[HeaderReset] = resetSeconds.ToString(CultureInfo.InvariantCulture);

        if (decision.RetryAfterUtc is { } retryAfter)
        {
            var retrySeconds = Math.Max(
                0,
                (int)Math.Ceiling((retryAfter - decision.Timestamp).TotalSeconds));
            headers[HeaderRetryAfter] = retrySeconds.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            headers.Remove(HeaderRetryAfter);
        }
    }
}
