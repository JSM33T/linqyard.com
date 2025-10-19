using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

/// <summary>
/// Defines operations for retrieving tier metadata and upgrading a user's tier
/// using Razorpay as the payment provider.
/// </summary>
public interface ITierService
{
    /// <summary>
    /// Gets the list of purchasable tiers along with pricing information.
    /// </summary>
    Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current active tier for a specific user.
    /// </summary>
    Task<UserTierInfo?> GetUserActiveTierAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Razorpay order so that the user can complete payment for an upgrade.
    /// </summary>
    Task<TierOrderResponse> CreateUpgradeOrderAsync(
        Guid userId,
        TierUpgradeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms payment and upgrades the user's tier.
    /// </summary>
    Task<TierUpgradeConfirmationResponse> ConfirmUpgradeAsync(
        Guid userId,
        TierPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a coupon for a particular tier billing period and computes the discounted amount.
    /// </summary>
    Task<TierCouponPreviewResponse> PreviewCouponAsync(
        TierCouponPreviewRequest request,
        CancellationToken cancellationToken = default);
}
