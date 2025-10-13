using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces;

public interface IViewTelemetryRepository
{
    /// <summary>
    /// Record a profile view event
    /// </summary>
    Task RecordProfileViewAsync(RecordProfileViewRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated profile views for a user
    /// </summary>
    Task<ProfileViewsPageResponse> GetProfileViewsAsync(GetProfileViewsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated statistics for profile views
    /// </summary>
    Task<ProfileViewStatsResponse> GetProfileViewStatsAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get source breakdown for profile views
    /// </summary>
    Task<IReadOnlyList<SourceBreakdownResponse>> GetSourceBreakdownAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get geographic distribution of views
    /// </summary>
    Task<IReadOnlyList<GeographicDistributionResponse>> GetGeographicDistributionAsync(Guid profileUserId, DateTimeOffset? startDate, DateTimeOffset? endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total view count for a profile
    /// </summary>
    Task<long> GetTotalViewCountAsync(Guid profileUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unique visitor count for a profile
    /// </summary>
    Task<long> GetUniqueVisitorCountAsync(Guid profileUserId, CancellationToken cancellationToken = default);
}
