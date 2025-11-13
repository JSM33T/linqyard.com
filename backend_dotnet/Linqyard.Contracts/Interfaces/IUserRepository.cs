using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Provides persistence operations for querying and updating user records.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Returns the total number of users in the system.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The total user count.</returns>
        Task<int> GetUserCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the public profile information for a user by username.
        /// </summary>
        /// <param name="username">Public username to search for.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The public response or <c>null</c> if not found.</returns>
        Task<UserPublicResponse?> GetPublicByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the basic user record by username.
        /// </summary>
        /// <param name="username">Username to search for.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The basic response or <c>null</c> if not found.</returns>
        Task<UserBasicResponse?> GetUserByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the administrative view of users with pagination.
        /// </summary>
        /// <param name="search">Optional free-text search value.</param>
        /// <param name="page">Page number to retrieve.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A tuple containing the users and total count.</returns>
        Task<(IReadOnlyList<AdminUserListItemResponse> Users, long Total)> SearchAdminUsersAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves detailed administrative information for a specific user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The admin user details or <c>null</c> if not found.</returns>
        Task<AdminUserDetailsResponse?> GetAdminUserDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the administrative fields for a user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="request">Update payload containing the desired changes.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated admin user response or <c>null</c> if the user does not exist.</returns>
        Task<AdminUserDetailsResponse?> UpdateAdminUserAsync(
            Guid userId,
            AdminUpdateUserRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns a tier to the specified user on behalf of an admin.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="request">Assignment payload describing the tier and scheduling.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated admin user response or <c>null</c> if the user does not exist.</returns>
        Task<AdminUserDetailsResponse?> AssignAdminUserTierAsync(
            Guid userId,
            AdminUpgradeUserTierRequest request,
            CancellationToken cancellationToken = default);
    }
}
