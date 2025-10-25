using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Linqyard.Contracts.Requests;
using Linqyard.Contracts.Responses;

namespace Linqyard.Contracts.Interfaces
{
    /// <summary>
    /// Provides business operations for managing link groups and their ordering.
    /// </summary>
    public interface IGroupService
    {
        /// <summary>
        /// Returns the link groups owned by the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the group owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A collection of link groups.</returns>
        Task<IReadOnlyList<LinkGroupResponse>> GetGroupsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the link groups that belong to a user identified by username.
        /// </summary>
        /// <param name="username">Public username of the profile owner.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A collection of link groups.</returns>
        Task<IReadOnlyList<LinkGroupResponse>> GetGroupsByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new link group on behalf of the specified user.
        /// </summary>
        /// <param name="userId">Unique identifier of the requesting user.</param>
        /// <param name="request">Payload describing the group to create.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The created link group.</returns>
        Task<LinkGroupResponse> CreateGroupAsync(
            Guid userId,
            CreateGroupRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing link group's details.
        /// </summary>
        /// <param name="groupId">Identifier of the group to edit.</param>
        /// <param name="userId">Identifier of the user performing the edit.</param>
        /// <param name="isAdmin">Whether the caller is an administrator.</param>
        /// <param name="request">Payload describing updated properties.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link group.</returns>
        Task<LinkGroupResponse> UpdateGroupAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            UpdateGroupRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggles the active status of a link group.
        /// </summary>
        /// <param name="groupId">Identifier of the group to update.</param>
        /// <param name="userId">Identifier of the user performing the action.</param>
        /// <param name="isAdmin">Whether the caller is an administrator.</param>
        /// <param name="isActive">New active flag for the group.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The updated link group.</returns>
        Task<LinkGroupResponse> UpdateGroupStatusAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            bool isActive,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a link group permanently.
        /// </summary>
        /// <param name="groupId">Identifier of the group to delete.</param>
        /// <param name="userId">Identifier of the user performing the delete.</param>
        /// <param name="isAdmin">Whether the caller is an administrator.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        Task DeleteGroupAsync(
            Guid groupId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reorders a user's link groups according to the provided sequence.
        /// </summary>
        /// <param name="userId">Identifier of the group owner.</param>
        /// <param name="items">Sequence instructions for each group.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The final resequencing result.</returns>
        Task<GroupResequenceResult> ResequenceGroupsAsync(
            Guid userId,
            IReadOnlyList<GroupResequenceItemRequest> items,
            CancellationToken cancellationToken = default);
    }
}

