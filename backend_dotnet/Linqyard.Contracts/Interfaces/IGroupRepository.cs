using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Defines methods for managing link groups associated with users, including CRUD operations and resequencing.
    /// </summary>
    public interface IGroupRepository
    {
        /// <summary>
        /// Retrieves all link groups associated with the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of link group responses.</returns>
        Task<IReadOnlyList<LinkGroupResponse>> GetGroupsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all link groups for a user identified by their username.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A read-only list of link group responses.</returns>
        Task<IReadOnlyList<LinkGroupResponse>> GetGroupsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the active tier ID associated with the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The active tier ID if available; otherwise, <c>null</c>.</returns>
        Task<int?> GetActiveTierIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the total number of groups created by the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The count of link groups belonging to the user.</returns>
        Task<int> GetUserGroupCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new link group for the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the user creating the group.</param>
        /// <param name="request">Request object containing group creation details.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created link group response.</returns>
        Task<LinkGroupResponse> CreateGroupAsync(
            Guid userId,
            CreateGroupRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing link group's properties.
        /// </summary>
        /// <param name="groupId">Unique identifier of the group to update.</param>
        /// <param name="userId">Unique identifier of the user performing the update.</param>
        /// <param name="isAdmin">Indicates whether the user has administrative privileges.</param>
        /// <param name="request">Request object containing updated group details.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link group response.</returns>
        Task<LinkGroupResponse> UpdateGroupAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            UpdateGroupRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the active status of a specific link group.
        /// </summary>
        /// <param name="groupId">Unique identifier of the group to update.</param>
        /// <param name="userId">Unique identifier of the user performing the update.</param>
        /// <param name="isAdmin">Indicates whether the user has administrative privileges.</param>
        /// <param name="isActive">The new active status for the group.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link group response.</returns>
        Task<LinkGroupResponse> UpdateGroupStatusAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            bool isActive,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the specified link group.
        /// </summary>
        /// <param name="groupId">Unique identifier of the group to delete.</param>
        /// <param name="userId">Unique identifier of the user performing the deletion.</param>
        /// <param name="isAdmin">Indicates whether the user has administrative privileges.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task DeleteGroupAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the display order (sequence) of a user's link groups.
        /// </summary>
        /// <param name="userId">Unique identifier of the user.</param>
        /// <param name="items">Collection of group resequence item requests defining the new order.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The result of the resequencing operation, including affected group data.</returns>
        Task<GroupResequenceResult> ResequenceGroupsAsync(
            Guid userId,
            IReadOnlyList<GroupResequenceItemRequest> items,
            CancellationToken cancellationToken = default);
    }
}
