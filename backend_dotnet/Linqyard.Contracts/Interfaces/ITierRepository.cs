using System;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

/// <summary>
/// Defines data access operations for tier information, billing cycles, and coupons.
/// </summary>
public interface ITierRepository
{
    Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
        CancellationToken cancellationToken = default);

    Task<UserTierInfo?> GetUserActiveTierAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<TierOrderResponse> CreateUpgradeOrderAsync(
        Guid userId,
        TierUpgradeRequest request,
        CancellationToken cancellationToken = default);

    Task<TierUpgradeConfirmationResponse> ConfirmUpgradeAsync(
        Guid userId,
        TierPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default);

    Task<TierCouponPreviewResponse> PreviewCouponAsync(
        TierCouponPreviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TierAdminDetailsResponse>> GetAdminTiersAsync(
        CancellationToken cancellationToken = default);

    Task<TierAdminBillingCycleResponse> CreateBillingCycleAsync(
        TierBillingCycleCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<TierAdminBillingCycleResponse> UpdateBillingCycleAsync(
        int billingCycleId,
        TierBillingCycleUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteBillingCycleAsync(
        int billingCycleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CouponAdminResponse>> GetAdminCouponsAsync(
        CancellationToken cancellationToken = default);

    Task<CouponAdminResponse> CreateCouponAsync(
        CouponCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<CouponAdminResponse> UpdateCouponAsync(
        Guid couponId,
        CouponUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCouponAsync(
        Guid couponId,
        CancellationToken cancellationToken = default);
}

