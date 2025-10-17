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
public sealed record TierUpgradeRequest(string TierName, string BillingPeriod);

/// <summary>
/// Request payload for confirming a completed Razorpay payment.
/// </summary>
/// <param name="TierName">The tier purchased as part of the payment.</param>
/// <param name="RazorpayOrderId">The Razorpay order identifier returned during order creation.</param>
/// <param name="RazorpayPaymentId">The Razorpay payment identifier received by the client.</param>
/// <param name="RazorpaySignature">The HMAC signature returned by Razorpay for verification.</param>
/// <param name="BillingPeriod">Billing period used when the order was created.</param>
public sealed record TierPaymentConfirmationRequest(
    string TierName,
    string BillingPeriod,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);
