using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Defines methods for recording and retrieving analytics data related to link interactions.
    /// </summary>
    public interface IAnalyticsRepository
    {
        /// <summary>
        /// Records a link click event for analytics tracking.
        /// </summary>
        /// <param name="request">The request containing link click details.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task RecordLinkClickAsync(RecordLinkClickRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the total click counts for all links associated with a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of link click count responses.</returns>
        Task<IReadOnlyList<LinkClickCountResponse>> GetLinkClickCountsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves recent analytics events for a specific link owned by a user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="linkId">The unique identifier of the link.</param>
        /// <param name="take">The maximum number of events to retrieve.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A read-only list of analytics event responses, or <c>null</c> if no events are found.
        /// </returns>
        Task<IReadOnlyList<AnalyticsEventResponse>?> GetLinkEventsForUserAsync(Guid userId, Guid linkId, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the total click count for a user within a specified date range.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="fromInclusive">The start date (inclusive) of the period.</param>
        /// <param name="toInclusive">The end date (inclusive) of the period.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The total number of clicks recorded in the given time range.</returns>
        Task<long> GetClickCountAsync(Guid userId, DateTimeOffset fromInclusive, DateTimeOffset toInclusive, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the distinct user agent strings for all link clicks by a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of user agent strings associated with the user's clicks.</returns>
        Task<IReadOnlyList<string?>> GetUserAgentsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
