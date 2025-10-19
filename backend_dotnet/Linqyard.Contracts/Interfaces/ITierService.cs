using System;
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

    /// <summary>
    /// Retrieves full tier metadata for administrative management.
    /// </summary>
    Task<IReadOnlyList<TierAdminDetailsResponse>> GetAdminTiersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a billing cycle for a tier.
    /// </summary>
    Task<TierAdminBillingCycleResponse> CreateBillingCycleAsync(
        TierBillingCycleCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing billing cycle.
    /// </summary>
    Task<TierAdminBillingCycleResponse> UpdateBillingCycleAsync(
        int billingCycleId,
        TierBillingCycleUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a billing cycle.
    /// </summary>
    Task DeleteBillingCycleAsync(
        int billingCycleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists coupons for administrative management.
    /// </summary>
    Task<IReadOnlyList<CouponAdminResponse>> GetAdminCouponsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a coupon definition.
    /// </summary>
    Task<CouponAdminResponse> CreateCouponAsync(
        CouponCreateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing coupon.
    /// </summary>
    Task<CouponAdminResponse> UpdateCouponAsync(
        Guid couponId,
        CouponUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a coupon permanently.
    /// </summary>
    Task DeleteCouponAsync(
        Guid couponId,
        CancellationToken cancellationToken = default);
}
