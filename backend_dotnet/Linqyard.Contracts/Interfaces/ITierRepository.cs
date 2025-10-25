using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Defines data access operations for tier information, billing cycles, and coupons.
    /// </summary>
    public interface ITierRepository
    {
        /// <summary>
        /// Returns the list of purchasable tiers and their pricing metadata.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of tier details.</returns>
        Task<IReadOnlyList<TierDetailsResponse>> GetAvailableTiersAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the active tier for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The user's tier info, if any.</returns>
        Task<UserTierInfo?> GetUserActiveTierAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a new Razorpay order for a tier upgrade.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="request">Upgrade order payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created order response.</returns>
        Task<TierOrderResponse> CreateUpgradeOrderAsync(
            Guid userId,
            TierUpgradeRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Confirms an upgrade after Razorpay payment verification.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="request">Payment confirmation payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The confirmation response.</returns>
        Task<TierUpgradeConfirmationResponse> ConfirmUpgradeAsync(
            Guid userId,
            TierPaymentConfirmationRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Previews the effect of a coupon for the requested tier billing period.
        /// </summary>
        /// <param name="userId">Unique identifier of the user, if available.</param>
        /// <param name="request">Coupon preview payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The coupon preview response.</returns>
        Task<TierCouponPreviewResponse> PreviewCouponAsync(
            Guid? userId,
            TierCouponPreviewRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the administrative tier metadata.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of admin tier details.</returns>
        Task<IReadOnlyList<TierAdminDetailsResponse>> GetAdminTiersAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a billing cycle for a tier.
        /// </summary>
        /// <param name="request">Billing cycle creation payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created billing cycle.</returns>
        Task<TierAdminBillingCycleResponse> CreateBillingCycleAsync(
            TierBillingCycleCreateRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a billing cycle record.
        /// </summary>
        /// <param name="billingCycleId">Identifier of the cycle to update.</param>
        /// <param name="request">Billing cycle update payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated billing cycle.</returns>
        Task<TierAdminBillingCycleResponse> UpdateBillingCycleAsync(
            int billingCycleId,
            TierBillingCycleUpdateRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a billing cycle record.
        /// </summary>
        /// <param name="billingCycleId">Identifier of the cycle to delete.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task DeleteBillingCycleAsync(
            int billingCycleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the list of coupons for administrative management.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of coupons.</returns>
        Task<IReadOnlyList<CouponAdminResponse>> GetAdminCouponsAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a coupon definition.
        /// </summary>
        /// <param name="request">Coupon creation payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created coupon.</returns>
        Task<CouponAdminResponse> CreateCouponAsync(
            CouponCreateRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a coupon definition.
        /// </summary>
        /// <param name="couponId">Identifier of the coupon to update.</param>
        /// <param name="request">Coupon update payload.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated coupon.</returns>
        Task<CouponAdminResponse> UpdateCouponAsync(
            Guid couponId,
            CouponUpdateRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a coupon permanently.
        /// </summary>
        /// <param name="couponId">Identifier of the coupon to delete.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task DeleteCouponAsync(
            Guid couponId,
            CancellationToken cancellationToken = default);
    }
}
