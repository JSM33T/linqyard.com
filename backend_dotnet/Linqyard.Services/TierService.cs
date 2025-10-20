using System;
using Linqyard.Contracts.Interfaces;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Services;

public sealed class TierService : ITierService
{
    private readonly ITierRepository _tierRepository;

    public TierService(ITierRepository tierRepository)
    {
        _tierRepository = tierRepository ?? throw new ArgumentNullException(nameof(tierRepository));
    }

    public Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
        CancellationToken cancellationToken = default) =>
        _tierRepository.GetAvailableTiersAsync(cancellationToken);

    public Task<UserTierInfo?> GetUserActiveTierAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _tierRepository.GetUserActiveTierAsync(userId, cancellationToken);

    public Task<TierOrderResponse> CreateUpgradeOrderAsync(
        Guid userId,
        TierUpgradeRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.CreateUpgradeOrderAsync(userId, request, cancellationToken);

    public Task<TierUpgradeConfirmationResponse> ConfirmUpgradeAsync(
        Guid userId,
        TierPaymentConfirmationRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.ConfirmUpgradeAsync(userId, request, cancellationToken);

    public Task<TierCouponPreviewResponse> PreviewCouponAsync(
        Guid? userId,
        TierCouponPreviewRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.PreviewCouponAsync(userId, request, cancellationToken);

    public Task<IReadOnlyList<TierAdminDetailsResponse>> GetAdminTiersAsync(
        CancellationToken cancellationToken = default) =>
        _tierRepository.GetAdminTiersAsync(cancellationToken);

    public Task<TierAdminBillingCycleResponse> CreateBillingCycleAsync(
        TierBillingCycleCreateRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.CreateBillingCycleAsync(request, cancellationToken);

    public Task<TierAdminBillingCycleResponse> UpdateBillingCycleAsync(
        int billingCycleId,
        TierBillingCycleUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.UpdateBillingCycleAsync(billingCycleId, request, cancellationToken);

    public Task DeleteBillingCycleAsync(
        int billingCycleId,
        CancellationToken cancellationToken = default) =>
        _tierRepository.DeleteBillingCycleAsync(billingCycleId, cancellationToken);

    public Task<IReadOnlyList<CouponAdminResponse>> GetAdminCouponsAsync(
        CancellationToken cancellationToken = default) =>
        _tierRepository.GetAdminCouponsAsync(cancellationToken);

    public Task<CouponAdminResponse> CreateCouponAsync(
        CouponCreateRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.CreateCouponAsync(request, cancellationToken);

    public Task<CouponAdminResponse> UpdateCouponAsync(
        Guid couponId,
        CouponUpdateRequest request,
        CancellationToken cancellationToken = default) =>
        _tierRepository.UpdateCouponAsync(couponId, request, cancellationToken);

    public Task DeleteCouponAsync(
        Guid couponId,
        CancellationToken cancellationToken = default) =>
        _tierRepository.DeleteCouponAsync(couponId, cancellationToken);
}
