using System.Collections.Generic;
using System.Linq;

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

/// <summary>
/// Describes the available paid tiers and their pricing.
/// </summary>
public sealed class TierPricingOptions
{
    private Dictionary<string, TierPlanOptions> _plans = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// ISO 4217 currency code used for Razorpay interactions.
    /// </summary>
    public string Currency { get; set; } = "INR";

    /// <summary>
    /// Mapping between tier names (e.g. "plus") and their pricing information.
    /// </summary>
    public Dictionary<string, TierPlanOptions> Plans
    {
        get => _plans;
        set => _plans = value is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, TierPlanOptions>(
                value.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value ?? new TierPlanOptions(),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Pricing details for a specific tier.
/// </summary>
public sealed class TierPlanOptions
{
    private Dictionary<string, TierBillingCycleOptions> _billingCycles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Optional description override (not persisted to database).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Available billing cycles (e.g. monthly, yearly) for the tier.
    /// </summary>
    public Dictionary<string, TierBillingCycleOptions> BillingCycles
    {
        get => _billingCycles;
        set => _billingCycles = value is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, TierBillingCycleOptions>(
                value.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value ?? new TierBillingCycleOptions(),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Pricing details for a specific billing cycle (monthly, yearly, etc.).
/// </summary>
public sealed class TierBillingCycleOptions
{
    /// <summary>
    /// Price in the smallest currency unit required by Razorpay (for INR this is paise).
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Duration in months for which the tier should remain active.
    /// Use 0 to indicate no automatic expiry.
    /// </summary>
    public int DurationMonths { get; set; } = 1;

    /// <summary>
    /// Optional description override (not persisted to database).
    /// </summary>
    public string? Description { get; set; }
}
