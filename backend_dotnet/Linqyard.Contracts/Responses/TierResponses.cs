namespace Linqyard.Contracts.Responses;

/// <summary>
/// Describes a purchasable tier along with its available billing cycles.
/// </summary>
/// <param name="TierId">The unique identifier from the database.</param>
/// <param name="Name">Canonical name such as "free", "plus", or "pro".</param>
/// <param name="Description">Optional human readable description.</param>
/// <param name="Currency">ISO 4217 currency code (e.g. INR).</param>
/// <param name="Plans">Available billing cycles for the tier.</param>
public sealed record TierDetailsResponse(
    int TierId,
    string Name,
    string? Description,
    string Currency,
    IReadOnlyList<TierPlanDetailsResponse> Plans);

/// <summary>
/// Describes a single billing cycle option for a tier.
/// </summary>
/// <param name="BillingPeriod">Identifier such as "monthly" or "yearly".</param>
/// <param name="Amount">Price in the smallest currency unit (INR -> paise).</param>
/// <param name="DurationMonths">Duration of the plan in whole months.</param>
/// <param name="Description">Optional description override for the plan.</param>
public sealed record TierPlanDetailsResponse(
    string BillingPeriod,
    int Amount,
    int DurationMonths,
    string? Description);

/// <summary>
/// Response returned when an order is created with Razorpay.
/// </summary>
/// <param name="TierName">Tier that is being purchased.</param>
/// <param name="OrderId">Razorpay order id.</param>
/// <param name="Receipt">Internal receipt identifier (mirrors Razorpay order receipt).</param>
/// <param name="Amount">Price in the smallest currency unit.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="RazorpayKeyId">Publishable Razorpay key to be used by the frontend.</param>
/// <param name="BillingPeriod">Billing period associated with the order.</param>
/// <param name="SubtotalAmount">Original price before discounts are applied.</param>
/// <param name="DiscountAmount">Amount discounted in the smallest currency unit.</param>
/// <param name="CouponCode">Coupon code applied to the order, if any.</param>
public sealed record TierOrderResponse(
    string TierName,
    string BillingPeriod,
    string OrderId,
    string Receipt,
    int Amount,
    string Currency,
    string RazorpayKeyId,
    int SubtotalAmount,
    int DiscountAmount,
    string? CouponCode);

/// <summary>
/// Response returned after a successful payment confirmation and tier upgrade.
/// </summary>
/// <param name="Tier">The active tier details after upgrade.</param>
/// <param name="Message">Human readable status message.</param>
/// <param name="BillingPeriod">Billing period that was activated.</param>
public sealed record TierUpgradeConfirmationResponse(
    UserTierInfo Tier,
    string Message,
    string BillingPeriod);

/// <summary>
/// Response returned when validating a coupon against a tier billing cycle.
/// </summary>
/// <param name="CouponCode">Normalized coupon code.</param>
/// <param name="DiscountPercentage">Percentage discount offered by the coupon.</param>
/// <param name="DiscountAmount">Discount amount in the smallest currency unit.</param>
/// <param name="SubtotalAmount">Subtotal before the coupon discount.</param>
/// <param name="FinalAmount">Final amount after applying the coupon.</param>
/// <param name="Currency">ISO 4217 currency code.</param>
/// <param name="Description">Optional textual description of the coupon.</param>
/// <param name="ValidUntil">UTC timestamp representing coupon expiry, if any.</param>
public sealed record TierCouponPreviewResponse(
    string CouponCode,
    decimal DiscountPercentage,
    int DiscountAmount,
    int SubtotalAmount,
    int FinalAmount,
    string Currency,
    string? Description,
    DateTimeOffset? ValidUntil);

/// <summary>
/// Detailed tier information for admin consoles, including billing cycles.
/// </summary>
/// <param name="TierId">Database identifier.</param>
/// <param name="Name">Canonical tier name.</param>
/// <param name="Currency">Currency code used for pricing.</param>
/// <param name="Description">Optional tier description.</param>
/// <param name="BillingCycles">Full billing cycle list for the tier.</param>
public sealed record TierAdminDetailsResponse(
    int TierId,
    string Name,
    string Currency,
    string? Description,
    IReadOnlyList<TierAdminBillingCycleResponse> BillingCycles);

/// <summary>
/// Represents a billing cycle as viewed/edited from the admin console.
/// </summary>
/// <param name="Id">Primary key of the billing cycle.</param>
/// <param name="BillingPeriod">Identifier such as "monthly".</param>
/// <param name="Amount">Price in the smallest unit.</param>
/// <param name="DurationMonths">Duration of the plan in months.</param>
/// <param name="Description">Optional description override.</param>
/// <param name="IsActive">Indicates if the plan can be purchased.</param>
public sealed record TierAdminBillingCycleResponse(
    int Id,
    string BillingPeriod,
    int Amount,
    int DurationMonths,
    string? Description,
    bool IsActive);

/// <summary>
/// Coupon payload exposed to admin clients.
/// </summary>
/// <param name="Id">Coupon identifier.</param>
/// <param name="Code">Coupon code (case-insensitive).</param>
/// <param name="DiscountPercentage">Percent discount represented by the coupon.</param>
/// <param name="Description">Optional description.</param>
/// <param name="TierId">Tier the coupon is limited to (null for global).</param>
/// <param name="TierName">Human readable tier name if limited.</param>
/// <param name="MaxRedemptions">Maximum number of times this coupon can be redeemed.</param>
/// <param name="RedemptionCount">Number of completed redemptions.</param>
/// <param name="ValidFrom">Optional start time.</param>
/// <param name="ValidUntil">Optional expiration.</param>
/// <param name="IsActive">Whether the coupon can currently be used.</param>
/// <param name="CreatedAt">Creation timestamp.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
public sealed record CouponAdminResponse(
    Guid Id,
    string Code,
    decimal DiscountPercentage,
    string? Description,
    int? TierId,
    string? TierName,
    int? MaxRedemptions,
    int RedemptionCount,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
