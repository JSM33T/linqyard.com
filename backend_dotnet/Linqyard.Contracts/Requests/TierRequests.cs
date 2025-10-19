namespace Linqyard.Contracts.Requests;

/// <summary>
/// Request payload for initiating a tier upgrade order.
/// </summary>
/// <param name="TierName">
/// The canonical tier name (e.g. "plus" or "pro") that the user wants to upgrade to.
/// </param>
/// <param name="BillingPeriod">
/// Billing period identifier (for example, "monthly" or "yearly") corresponding to a configured plan.
/// </param>
/// <param name="CouponCode">Optional coupon code to apply for the order.</param>
public sealed record TierUpgradeRequest(string TierName, string BillingPeriod, string? CouponCode = null);

/// <summary>
/// Request payload for confirming a completed Razorpay payment.
/// </summary>
/// <param name="TierName">The tier purchased as part of the payment.</param>
/// <param name="RazorpayOrderId">The Razorpay order identifier returned during order creation.</param>
/// <param name="RazorpayPaymentId">The Razorpay payment identifier received by the client.</param>
/// <param name="RazorpaySignature">The HMAC signature returned by Razorpay for verification.</param>
/// <param name="BillingPeriod">Billing period used when the order was created.</param>
/// <param name="CouponCode">Optional coupon code that was applied during checkout.</param>
public sealed record TierPaymentConfirmationRequest(
    string TierName,
    string BillingPeriod,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature,
    string? CouponCode = null);

/// <summary>
/// Request payload for validating a coupon before initiating checkout.
/// </summary>
/// <param name="TierName">Canonical tier name that the coupon should apply to.</param>
/// <param name="BillingPeriod">Billing period identifier, e.g. "monthly".</param>
/// <param name="CouponCode">Coupon code entered by the customer.</param>
public sealed record TierCouponPreviewRequest(
    string TierName,
    string BillingPeriod,
    string CouponCode);

/// <summary>
/// Request payload for creating a new billing cycle for a tier.
/// </summary>
/// <param name="TierId">Tier to which the billing cycle belongs.</param>
/// <param name="BillingPeriod">Identifier such as "monthly" or "yearly".</param>
/// <param name="Amount">Price expressed in the smallest currency unit.</param>
/// <param name="DurationMonths">Duration of the plan in months.</param>
/// <param name="Description">Optional description override.</param>
/// <param name="IsActive">Whether the billing cycle should be immediately purchasable.</param>
public sealed record TierBillingCycleCreateRequest(
    int TierId,
    string BillingPeriod,
    int Amount,
    int DurationMonths,
    string? Description,
    bool IsActive);

/// <summary>
/// Request payload for updating an existing tier billing cycle.
/// </summary>
/// <param name="BillingPeriod">Identifier such as "monthly" or "yearly".</param>
/// <param name="Amount">Price expressed in the smallest currency unit.</param>
/// <param name="DurationMonths">Duration of the plan in months.</param>
/// <param name="Description">Optional description override.</param>
/// <param name="IsActive">Whether the billing cycle should be purchasable.</param>
public sealed record TierBillingCycleUpdateRequest(
    string BillingPeriod,
    int Amount,
    int DurationMonths,
    string? Description,
    bool IsActive);

/// <summary>
/// Request payload for creating a coupon.
/// </summary>
/// <param name="Code">Coupon code (case-insensitive).</param>
/// <param name="DiscountPercentage">Discount percentage between 0 and 100.</param>
/// <param name="Description">Optional description.</param>
/// <param name="TierId">Optional tier limitation.</param>
/// <param name="MaxRedemptions">Maximum allowed redemptions.</param>
/// <param name="ValidFrom">Optional start date.</param>
/// <param name="ValidUntil">Optional expiration date.</param>
/// <param name="IsActive">Whether the coupon is active.</param>
public sealed record CouponCreateRequest(
    string Code,
    decimal DiscountPercentage,
    string? Description,
    int? TierId,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    bool IsActive);

/// <summary>
/// Request payload for updating an existing coupon.
/// </summary>
/// <param name="DiscountPercentage">Discount percentage between 0 and 100.</param>
/// <param name="Description">Optional description.</param>
/// <param name="TierId">Optional tier limitation.</param>
/// <param name="MaxRedemptions">Maximum allowed redemptions.</param>
/// <param name="ValidFrom">Optional start date.</param>
/// <param name="ValidUntil">Optional expiration date.</param>
/// <param name="IsActive">Whether the coupon is active.</param>
public sealed record CouponUpdateRequest(
    decimal DiscountPercentage,
    string? Description,
    int? TierId,
    int? MaxRedemptions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidUntil,
    bool IsActive);
