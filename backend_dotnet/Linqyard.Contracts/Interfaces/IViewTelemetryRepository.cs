using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Defines data access operations for recording and querying profile view telemetry.
    /// </summary>
    public interface IViewTelemetryRepository
    {
        /// <summary>
        /// Records a profile view event.
        /// </summary>
        /// <param name="request">Telemetry payload describing the view.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task RecordProfileViewAsync(
            RecordProfileViewRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paginated profile views for a user.
        /// </summary>
        /// <param name="request">Pagination request parameters.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The paginated view response.</returns>
        Task<ProfileViewsPageResponse> GetProfileViewsAsync(
            GetProfileViewsRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets aggregated statistics for profile views within the optional time range.
        /// </summary>
        /// <param name="profileUserId">Unique identifier of the profile owner.</param>
        /// <param name="startDate">Inclusive start of the time range.</param>
        /// <param name="endDate">Inclusive end of the time range.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The aggregated stats response.</returns>
        Task<ProfileViewStatsResponse> GetProfileViewStatsAsync(
            Guid profileUserId,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the traffic source breakdown for profile views.
        /// </summary>
        /// <param name="profileUserId">Unique identifier of the profile owner.</param>
        /// <param name="startDate">Inclusive start of the time range.</param>
        /// <param name="endDate">Inclusive end of the time range.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of source breakdown entries.</returns>
        Task<IReadOnlyList<SourceBreakdownResponse>> GetSourceBreakdownAsync(
            Guid profileUserId,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the geographic distribution of views.
        /// </summary>
        /// <param name="profileUserId">Unique identifier of the profile owner.</param>
        /// <param name="startDate">Inclusive start of the time range.</param>
        /// <param name="endDate">Inclusive end of the time range.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list describing geographic distribution.</returns>
        Task<IReadOnlyList<GeographicDistributionResponse>> GetGeographicDistributionAsync(
            Guid profileUserId,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total view count for a profile.
        /// </summary>
        /// <param name="profileUserId">Unique identifier of the profile owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The total view count.</returns>
        Task<long> GetTotalViewCountAsync(
            Guid profileUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the unique visitor count for a profile.
        /// </summary>
        /// <param name="profileUserId">Unique identifier of the profile owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The unique visitor count.</returns>
        Task<long> GetUniqueVisitorCountAsync(
            Guid profileUserId,
            CancellationToken cancellationToken = default);
    }
}
