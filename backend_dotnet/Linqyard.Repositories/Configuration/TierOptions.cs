namespace Linqyard.Repositories.Configuration;

/// <summary>
/// Razorpay credentials and API configuration.
/// </summary>
public sealed class RazorpaySettings
{
    /// <summary>
    /// Publishable Razorpay key that the client will use.
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Secret key used for server-side authentication/signature validation.
    /// </summary>
    public string KeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Optional webhook secret (not yet used, kept for future expansion).
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Base URL for Razorpay API. Defaults to the production endpoint.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.razorpay.com/v1/";
}
